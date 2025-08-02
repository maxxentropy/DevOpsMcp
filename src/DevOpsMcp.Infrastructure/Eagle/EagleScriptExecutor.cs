using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevOpsMcp.Domain.Eagle;
using DevOpsMcp.Domain.Interfaces;
using DevOpsMcp.Infrastructure.Configuration;
using ErrorOr;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Eagle;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace DevOpsMcp.Infrastructure.Eagle;

/// <summary>
/// Implementation of Eagle script executor with sandboxing and pooling
/// </summary>
public sealed class EagleScriptExecutor : IEagleScriptExecutor, IDisposable
{
    private readonly ILogger<EagleScriptExecutor> _logger;
    private readonly EagleOptions _options;
    private readonly ConcurrentBag<Interpreter> _interpreterPool;
    private readonly SemaphoreSlim _executionSemaphore;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _runningExecutions;
    private bool _disposed;

    public EagleScriptExecutor(
        ILogger<EagleScriptExecutor> logger,
        IOptions<EagleOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        _interpreterPool = new ConcurrentBag<Interpreter>();
        _executionSemaphore = new SemaphoreSlim(_options.MaxConcurrentExecutions);
        _runningExecutions = new ConcurrentDictionary<string, CancellationTokenSource>();
        
        // Pre-warm the pool
        PreWarmInterpreterPool();
    }

    public async Task<EagleExecutionResult> ExecuteAsync(
        DevOpsMcp.Domain.Eagle.ExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid().ToString();
        var startTime = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        
        await _executionSemaphore.WaitAsync(cancellationToken);
        Interpreter? interpreter = null;
        
        try
        {
            // Create cancellation source for timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(context.Timeout);
            _runningExecutions[executionId] = cts;
            
            // Get or create interpreter
            interpreter = await GetOrCreateInterpreterAsync(context.SecurityPolicy);
            
            // Setup execution context
            await SetupInterpreterContextAsync(interpreter, context);
            
            // Execute script with output capture
            var compilationStopwatch = Stopwatch.StartNew();
            global::Eagle._Components.Public.Result? eagleResult = null;
            int errorLine = 0;
            var outputCapture = new EagleOutputCapture();
            
            // Wrap the script to capture puts output
            var wrappedScript = WrapScriptForOutputCapture(context.Script);
            
            var returnCode = await Task.Run(() => 
                interpreter.EvaluateScript(wrappedScript, ref eagleResult, ref errorLine), 
                cts.Token);
            
            // Get captured output
            var capturedOutput = await GetCapturedOutputAsync(interpreter);
            compilationStopwatch.Stop();
            
            stopwatch.Stop();
            
            // Build metrics
            var metrics = new EagleExecutionMetrics
            {
                CompilationTime = compilationStopwatch.Elapsed,
                ExecutionTime = stopwatch.Elapsed - compilationStopwatch.Elapsed,
                CommandsExecuted = GetCommandCount(interpreter),
                MemoryUsageBytes = GC.GetTotalMemory(false),
                SecurityChecksPerformed = 0 // TODO: Track security checks
            };
            
            // Combine captured output with result
            var finalResult = !string.IsNullOrEmpty(capturedOutput) 
                ? capturedOutput 
                : eagleResult?.ToString();
            
            return new EagleExecutionResult
            {
                ExecutionId = executionId,
                IsSuccess = returnCode == ReturnCode.Ok,
                Result = finalResult,
                ErrorMessage = returnCode != ReturnCode.Ok ? eagleResult?.ToString() : null,
                StartTimeUtc = startTime,
                EndTimeUtc = DateTime.UtcNow,
                Metrics = metrics,
                ExitCode = (int)returnCode
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Script execution {ExecutionId} timed out", executionId);
            
            return new EagleExecutionResult
            {
                ExecutionId = executionId,
                IsSuccess = false,
                ErrorMessage = "Execution timed out",
                StartTimeUtc = startTime,
                EndTimeUtc = DateTime.UtcNow,
                Metrics = new EagleExecutionMetrics(),
                ExitCode = -1
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Script execution {ExecutionId} failed", executionId);
            
            return new EagleExecutionResult
            {
                ExecutionId = executionId,
                IsSuccess = false,
                ErrorMessage = $"Execution failed: {ex.Message}",
                StartTimeUtc = startTime,
                EndTimeUtc = DateTime.UtcNow,
                Metrics = new EagleExecutionMetrics(),
                ExitCode = -1
            };
        }
        finally
        {
            _runningExecutions.TryRemove(executionId, out _);
            
            if (interpreter != null)
            {
                ResetInterpreter(interpreter);
                _interpreterPool.Add(interpreter);
            }
            
            _executionSemaphore.Release();
        }
    }

    public async Task<ErrorOr<ValidationResult>> ValidateScriptAsync(
        string script,
        EagleSecurityPolicy policy,
        CancellationToken cancellationToken = default)
    {
        Interpreter? interpreter = null;
        
        try
        {
            interpreter = await GetOrCreateInterpreterAsync(policy);
            
            // Validate script by attempting to parse it
            global::Eagle._Components.Public.Result? parseResult = null;
            int errorLine = 0;
            
            // Use a minimal script wrapper to test parsing
            var testScript = $"if {{catch {{{script}}} parseError}} {{ set parseError }}";
            var returnCode = await Task.Run(() => 
                interpreter.EvaluateScript(testScript, ref parseResult, ref errorLine), 
                cancellationToken);
            
            if (returnCode == ReturnCode.Ok && string.IsNullOrEmpty(parseResult?.ToString()))
            {
                return new ValidationResult
                {
                    IsValid = true
                };
            }
            
            return new ValidationResult
            {
                IsValid = false,
                Errors = new[] { parseResult?.ToString() ?? "Parse error" }
            };
        }
        finally
        {
            if (interpreter != null)
            {
                ResetInterpreter(interpreter);
                _interpreterPool.Add(interpreter);
            }
        }
    }

    public Task<EagleCapabilities> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        // Return static capabilities for now
        return Task.FromResult(new EagleCapabilities
        {
            Version = "1.0.9787",
            AvailableCommands = new[] { "set", "puts", "if", "while", "foreach", "proc" },
            LoadedPackages = Array.Empty<string>(),
            SupportsClrIntegration = true,
            SupportsTcl = true
        });
    }

    public async Task<ErrorOr<Success>> CancelExecutionAsync(
        string executionId,
        CancellationToken cancellationToken = default)
    {
        if (_runningExecutions.TryGetValue(executionId, out var cts))
        {
            await cts.CancelAsync();
            return ErrorOr.Result.Success;
        }
        
        return Error.NotFound($"Execution {executionId} not found");
    }

    public Task<IReadOnlyList<EagleExecutionResult>> GetExecutionHistoryAsync(
        string? sessionId = null,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implement execution history tracking
        return Task.FromResult<IReadOnlyList<EagleExecutionResult>>(
            Array.Empty<EagleExecutionResult>());
    }

    private void PreWarmInterpreterPool()
    {
        for (int i = 0; i < _options.MinPoolSize; i++)
        {
            var interpreter = CreateSafeInterpreter(EagleSecurityPolicy.Standard);
            _interpreterPool.Add(interpreter);
        }
    }

    private async Task<Interpreter> GetOrCreateInterpreterAsync(EagleSecurityPolicy policy)
    {
        if (_interpreterPool.TryTake(out var interpreter))
        {
            // TODO: Verify interpreter matches security policy
            return interpreter;
        }
        
        return await Task.Run(() => CreateSafeInterpreter(policy));
    }

    private Interpreter CreateSafeInterpreter(EagleSecurityPolicy policy)
    {
        // Create interpreter using the documented API
        global::Eagle._Components.Public.Result? result = null;
        var interpreter = Interpreter.Create(ref result);
        
        #pragma warning disable CA1508 // Defensive null check for external API
        if (interpreter is null)
        {
            throw new System.InvalidOperationException($"Failed to create Eagle interpreter: {result}");
        }
        #pragma warning restore CA1508
        
        // Apply security restrictions
        ApplySecurityPolicy(interpreter, policy);
        
        return interpreter;
    }

    private void ApplySecurityPolicy(Interpreter interpreter, EagleSecurityPolicy policy)
    {
        // Remove restricted commands
        foreach (var command in policy.RestrictedCommands)
        {
            try
            {
                // TODO: Implement command hiding based on Eagle API
                // This may require using interpreter policies or custom command maps
                _logger.LogDebug("Command restriction for {Command} pending implementation", command);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to hide command {Command}", command);
            }
        }
        
        // TODO: Configure additional security settings
    }

    private async Task SetupInterpreterContextAsync(
        Interpreter interpreter,
        DevOpsMcp.Domain.Eagle.ExecutionContext context)
    {
        // Set variables
        foreach (var variable in context.Variables)
        {
            await Task.Run(() => 
                {
                    global::Eagle._Components.Public.Result? setResult = null;
                    int errorLine = 0;
                    // Use set command to create variables
                    var setScript = $"set {variable.Key} {{$variable.Value}}";
                    interpreter.EvaluateScript(setScript, ref setResult, ref errorLine);
                });
        }
        
        // TODO: Import packages, set working directory, etc.
    }

    private void ResetInterpreter(Interpreter interpreter)
    {
        try
        {
            // Reset interpreter state
            // Reset interpreter state - Eagle may not have a direct Reset method
            // Clear variables and restore to clean state
            global::Eagle._Components.Public.Result? resetResult = null;
            int errorLine = 0;
            interpreter.EvaluateScript("unset -nocomplain {*}[info vars]", ref resetResult, ref errorLine);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to reset interpreter");
            // Dispose and don't return to pool
            (interpreter as IDisposable)?.Dispose();
        }
    }

    private int GetCommandCount(Interpreter interpreter)
    {
        // TODO: Get actual command count from interpreter
        return 0;
    }

    private string WrapScriptForOutputCapture(string script)
    {
        // Escape the script for embedding
        var escapedScript = script.Replace("\\", "\\\\").Replace("\"", "\\\"");
        
        // Create a wrapper that captures all output
        return @"
# Initialize output capture variable
set ::_eagle_output """"

# Override puts to capture output
rename puts ::_original_puts
proc puts {args} {
    if {[llength $args] == 0} {
        append ::_eagle_output ""\n""
    } elseif {[llength $args] == 1} {
        append ::_eagle_output [lindex $args 0]\n
    } else {
        # Handle -nonewline option
        if {[lindex $args 0] eq ""-nonewline""} {
            append ::_eagle_output [lindex $args 1]
        } else {
            append ::_eagle_output [join $args "" ""]\n
        }
    }
}

# Execute the user script
set ::_eagle_script_result [catch {
    eval """ + escapedScript + @"""
} ::_eagle_script_error]

# Restore original puts
rename puts """"
rename ::_original_puts puts

# Return the result or error
if {$::_eagle_script_result == 0} {
    # If there's captured output, return it; otherwise return the last result
    if {[string length $::_eagle_output] > 0} {
        string trimright $::_eagle_output
    } else {
        set ::_eagle_script_error
    }
} else {
    error $::_eagle_script_error
}";
    }

    private async Task<string> GetCapturedOutputAsync(Interpreter interpreter)
    {
        try
        {
            // Try to get the captured output variable
            global::Eagle._Components.Public.Result? outputResult = null;
            int errorLine = 0;
            
            var code = await Task.Run(() =>
                interpreter.EvaluateScript("info exists ::_eagle_output", ref outputResult, ref errorLine));
            
            if (code == ReturnCode.Ok && outputResult?.ToString() == "1")
            {
                // Variable exists, get its value
                outputResult = null;
                code = await Task.Run(() =>
                    interpreter.EvaluateScript("set ::_eagle_output", ref outputResult, ref errorLine));
                
                if (code == ReturnCode.Ok)
                {
                    return outputResult?.ToString() ?? string.Empty;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to get captured output");
        }
        
        return string.Empty;
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        while (_interpreterPool.TryTake(out var interpreter))
        {
            (interpreter as IDisposable)?.Dispose();
        }
        
        _executionSemaphore?.Dispose();
        _disposed = true;
    }
}