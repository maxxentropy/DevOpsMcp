using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevOpsMcp.Domain.Eagle;
using DevOpsMcp.Domain.Interfaces;
using DevOpsMcp.Infrastructure.Configuration;
using ErrorOr;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Eagle;
using Eagle._Components.Public;
using Eagle._Constants;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using _Result = Eagle._Components.Public.Result;

namespace DevOpsMcp.Infrastructure.Eagle;

/// <summary>
/// Implementation of Eagle script executor with sandboxing and pooling
/// </summary>
public sealed class EagleScriptExecutor : IEagleScriptExecutor, IDisposable
{
    private readonly ILogger<EagleScriptExecutor> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly EagleOptions _options;
    private readonly InterpreterPool _interpreterPool;
    private readonly SemaphoreSlim _executionSemaphore;
    private readonly ConcurrentDictionary<string, CancellationTokenSource> _runningExecutions;
    private readonly IEagleContextProvider _contextProvider;
    private readonly IEagleOutputFormatter _outputFormatter;
    private readonly IEagleSecurityMonitor _securityMonitor;
    private readonly IExecutionHistoryStore _historyStore;
    private bool _disposed;

    public EagleScriptExecutor(
        ILogger<EagleScriptExecutor> logger,
        IServiceProvider serviceProvider,
        IOptions<EagleOptions> options,
        IEagleContextProvider contextProvider,
        IEagleOutputFormatter outputFormatter,
        IEagleSecurityMonitor securityMonitor,
        IExecutionHistoryStore historyStore)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _contextProvider = contextProvider;
        _outputFormatter = outputFormatter;
        _securityMonitor = securityMonitor;
        _historyStore = historyStore;
        _interpreterPool = new InterpreterPool(
            serviceProvider.GetRequiredService<ILogger<InterpreterPool>>(), 
            options, 
            securityMonitor);
        _executionSemaphore = new SemaphoreSlim(_options.MaxConcurrentExecutions);
        _runningExecutions = new ConcurrentDictionary<string, CancellationTokenSource>();
    }

    public async Task<EagleExecutionResult> ExecuteAsync(
        DevOpsMcp.Domain.Eagle.ExecutionContext context,
        CancellationToken cancellationToken = default)
    {
        var executionId = Guid.NewGuid().ToString();
        var startTime = DateTime.UtcNow;
        var stopwatch = Stopwatch.StartNew();
        
        await _executionSemaphore.WaitAsync(cancellationToken);
        PooledInterpreter? pooledInterpreter = null;
        
        try
        {
            // Create cancellation source for timeout
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(context.Timeout);
            _runningExecutions[executionId] = cts;
            
            // Get interpreter from pool
            pooledInterpreter = await _interpreterPool.AcquireAsync(context.SecurityPolicy, cts.Token);
            var interpreter = pooledInterpreter.Interpreter;
            
            // Configure security tracking for this execution
            ConfigureSecurityTracking(executionId, context.SecurityPolicy);
            
            // Setup execution context
            await SetupInterpreterContextAsync(interpreter, context);

            await Task.Run(() =>
            {
                int setOk = 0;
                _Result? result = null;

                interpreter.SetVariableValues(
                    new Dictionary<string, object>(context.Variables),
                    true, ref setOk, ref result);
            }, cancellationToken);

            // Execute script with a simple output capture wrapper
            var compilationStopwatch = Stopwatch.StartNew();
            _Result? eagleResult = null;
            int errorLine = 0;
            
            // Create a simple wrapper that captures puts output
            var captureScript = $@"
set _output """"
proc capture_puts {{args}} {{
    global _output
    if {{[llength $args] == 1}} {{
        append _output [lindex $args 0]\n
    }} elseif {{[llength $args] == 2 && [lindex $args 0] eq ""-nonewline""}} {{
        append _output [lindex $args 1]
    }} else {{
        append _output [join $args "" ""]\n
    }}
}}

# Temporarily replace puts
rename puts _original_puts
rename capture_puts puts

# Execute user script and capture result
set _result [catch {{{context.Script}}} _error]

# Restore original puts
rename puts capture_puts
rename _original_puts puts

# Return output if any, otherwise return the result/error
if {{$_result == 0}} {{
    if {{[string length $_output] > 0}} {{
        string trimright $_output
    }} else {{
        set _error
    }}
}} else {{
    error $_error
}}";
            
            var returnCode = await Task.Run(() => 
                interpreter.EvaluateScript(captureScript, ref eagleResult, ref errorLine), 
                cts.Token);
            compilationStopwatch.Stop();
            
            stopwatch.Stop();
            
            // Get security metrics for this execution
            var sessionMetrics = _securityMonitor.GetSessionMetrics(executionId);
            
            // Build metrics
            var metrics = new EagleExecutionMetrics
            {
                CompilationTime = compilationStopwatch.Elapsed,
                ExecutionTime = stopwatch.Elapsed - compilationStopwatch.Elapsed,
                CommandsExecuted = GetCommandCount(interpreter),
                MemoryUsageBytes = GC.GetTotalMemory(false),
                SecurityChecksPerformed = sessionMetrics.TotalEvents
            };
            
            // Process result with automatic structured output detection
            string? processedResult = null;
            FormattedOutput? formattedOutput = null;
            
            if (eagleResult != null)
            {
                var resultStr = eagleResult.ToString() ?? string.Empty;
                
                // Automatic structured output detection for Phase 1.2
                if (returnCode == ReturnCode.Ok && !string.IsNullOrWhiteSpace(resultStr))
                {
                    // Try to detect if the result is a Tcl list or dictionary
                    var structuredJson = TryConvertToStructuredOutput(interpreter, resultStr);
                    if (structuredJson != null)
                    {
                        processedResult = structuredJson;
                        
                        // If we detected structured output and no format was specified, 
                        // automatically format as JSON
                        if (context.OutputFormat == OutputFormat.Plain)
                        {
                            formattedOutput = new FormattedOutput
                            {
                                Format = OutputFormat.Json,
                                Content = structuredJson
                            };
                        }
                    }
                    else
                    {
                        processedResult = resultStr;
                    }
                }
                else
                {
                    processedResult = resultStr;
                }
                
                // Apply requested formatting
                if (context.OutputFormat != OutputFormat.Plain && formattedOutput == null)
                {
                    formattedOutput = await _outputFormatter.FormatAsync(
                        processedResult, 
                        context.OutputFormat);
                }
            }
            
            var result = new EagleExecutionResult
            {
                ExecutionId = executionId,
                IsSuccess = returnCode == ReturnCode.Ok,
                Result = processedResult,
                ErrorMessage = returnCode != ReturnCode.Ok ? eagleResult?.ToString() : null,
                StartTimeUtc = startTime,
                EndTimeUtc = DateTime.UtcNow,
                Metrics = metrics,
                ExitCode = (int)returnCode,
                SessionId = context.SessionId,
                FormattedOutput = formattedOutput
            };
            
            // Track execution in history
            // Track execution in history
            var historyEntry = new ExecutionHistoryEntry
            {
                ExecutionId = result.ExecutionId,
                SessionId = context.SessionId ?? string.Empty,
                Script = context.Script ?? string.Empty,
                Result = result.Result ?? string.Empty,
                Success = result.IsSuccess,
                StartTime = result.StartTimeUtc,
                EndTime = result.EndTimeUtc,
                ErrorMessage = result.ErrorMessage,
                ExecutionTime = result.Metrics?.ExecutionTime ?? TimeSpan.Zero,
                MemoryUsageBytes = result.Metrics?.MemoryUsageBytes ?? 0
            };
            await _historyStore.AddExecutionAsync(historyEntry);
            
            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Script execution {ExecutionId} timed out", executionId);
            
            var timeoutResult = new EagleExecutionResult
            {
                ExecutionId = executionId,
                IsSuccess = false,
                ErrorMessage = "Execution timed out",
                StartTimeUtc = startTime,
                EndTimeUtc = DateTime.UtcNow,
                Metrics = new EagleExecutionMetrics(),
                ExitCode = -1,
                SessionId = context.SessionId
            };
            
            // Track timeout in history
            var timeoutEntry = new ExecutionHistoryEntry
            {
                ExecutionId = timeoutResult.ExecutionId,
                SessionId = context.SessionId ?? string.Empty,
                Script = context.Script ?? string.Empty,
                Result = timeoutResult.Result ?? string.Empty,
                Success = timeoutResult.IsSuccess,
                StartTime = timeoutResult.StartTimeUtc,
                EndTime = timeoutResult.EndTimeUtc,
                ErrorMessage = timeoutResult.ErrorMessage,
                ExecutionTime = timeoutResult.Metrics?.ExecutionTime ?? TimeSpan.Zero,
                MemoryUsageBytes = timeoutResult.Metrics?.MemoryUsageBytes ?? 0
            };
            await _historyStore.AddExecutionAsync(timeoutEntry);
            return timeoutResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Script execution {ExecutionId} failed", executionId);
            
            var errorResult = new EagleExecutionResult
            {
                ExecutionId = executionId,
                IsSuccess = false,
                ErrorMessage = $"Execution failed: {ex.Message}",
                StartTimeUtc = startTime,
                EndTimeUtc = DateTime.UtcNow,
                Metrics = new EagleExecutionMetrics(),
                ExitCode = -1,
                SessionId = context.SessionId
            };
            
            // Track error in history
            var errorEntry = new ExecutionHistoryEntry
            {
                ExecutionId = errorResult.ExecutionId,
                SessionId = context.SessionId ?? string.Empty,
                Script = context.Script ?? string.Empty,
                Result = errorResult.Result ?? string.Empty,
                Success = errorResult.IsSuccess,
                StartTime = errorResult.StartTimeUtc,
                EndTime = errorResult.EndTimeUtc,
                ErrorMessage = errorResult.ErrorMessage,
                ExecutionTime = errorResult.Metrics?.ExecutionTime ?? TimeSpan.Zero,
                MemoryUsageBytes = errorResult.Metrics?.MemoryUsageBytes ?? 0
            };
            await _historyStore.AddExecutionAsync(errorEntry);
            return errorResult;
        }
        finally
        {
            _runningExecutions.TryRemove(executionId, out _);
            
            if (pooledInterpreter != null)
            {
                _interpreterPool.Release(pooledInterpreter, hadError: !pooledInterpreter.IsActive);
            }
            
            _executionSemaphore.Release();
        }
    }

    public async Task<ErrorOr<ValidationResult>> ValidateScriptAsync(
        string script,
        EagleSecurityPolicy policy,
        CancellationToken cancellationToken = default)
    {
        PooledInterpreter? pooledInterpreter = null;
        
        try
        {
            pooledInterpreter = await _interpreterPool.AcquireAsync(policy, cancellationToken);
            var interpreter = pooledInterpreter.Interpreter;

            // Validate script by attempting to parse it
            _Result? error = null;
            IParseState? parseState = null; /* NOT USED */
            TokenList? tokens = null; /* NOT USED */

            // Use a minimal script wrapper to test parsing
            var returnCode = await Task.Run(() =>
                Parser.ParseScript(interpreter, null,
                    Parser.StartLine, script, 0, Length.Invalid,
                    EngineFlags.None, SubstitutionFlags.Default,
                    false, false, false, false, ref parseState,
                    ref tokens, ref error),
                cancellationToken);

            if (returnCode == ReturnCode.Ok)
            {
                return new ValidationResult
                {
                    IsValid = true
                };
            }

            return new ValidationResult
            {
                IsValid = false,
                Errors = new[] { (error != null) ? error.ToString() : String.Empty }
            };
        }
        finally
        {
            if (pooledInterpreter != null)
            {
                _interpreterPool.Release(pooledInterpreter);
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

    public async Task<IReadOnlyList<EagleExecutionResult>> GetExecutionHistoryAsync(
        string? sessionId = null,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(sessionId))
        {
            var entries = await _historyStore.GetSessionHistoryAsync(sessionId);
            return entries.Take(limit).Select(ConvertToExecutionResult).ToList();
        }
        else
        {
            var entries = await _historyStore.GetRecentHistoryAsync(limit);
            return entries.Select(ConvertToExecutionResult).ToList();
        }
    }
    
    private static EagleExecutionResult ConvertToExecutionResult(ExecutionHistoryEntry entry)
    {
        return new EagleExecutionResult
        {
            ExecutionId = entry.ExecutionId,
            IsSuccess = entry.Success,
            Result = entry.Result,
            ErrorMessage = entry.ErrorMessage,
            StartTimeUtc = entry.StartTime,
            EndTimeUtc = entry.EndTime,
            Metrics = new EagleExecutionMetrics
            {
                ExecutionTime = entry.ExecutionTime,
                MemoryUsageBytes = entry.MemoryUsageBytes,
                CommandsExecuted = 0, // Not stored in history entry
                CompilationTime = TimeSpan.Zero, // Not stored in history entry
                VariablesCreated = 0,
                ProceduresDefined = 0,
                SecurityChecksPerformed = 0,
                CustomMetrics = new Dictionary<string, object>()
            },
            ExitCode = entry.Success ? 0 : -1,
            SecurityViolations = Array.Empty<string>(),
            FormattedOutput = null
        };
    }

    private async Task SetupInterpreterContextAsync(
        Interpreter interpreter,
        DevOpsMcp.Domain.Eagle.ExecutionContext context)
    {
        await Task.Run(() =>
        {
            int setOk = 0;
            _Result? result = null;

            interpreter.SetVariableValues(
                new Dictionary<string, object>(context.Variables),
                true, ref setOk, ref result);
        });

        // Import requested packages
        await ImportPackagesAsync(interpreter, context.ImportedPackages);
        
        // Set working directory
        await SetWorkingDirectoryAsync(interpreter, context.WorkingDirectory);
        
        // Inject environment variables
        await InjectEnvironmentVariablesAsync(interpreter, context.EnvironmentVariables);

        // Inject rich context commands
        // Wrap the Eagle interpreter in an adapter
        var interpreterAdapter = new EagleInterpreterAdapter(interpreter);
        _contextProvider.InjectRichContext(interpreterAdapter, context.DevOpsContext);
        
        // Inject output formatting commands
        // Pass the already-created adapter to maintain consistency
        _outputFormatter.InjectOutputCommands(interpreterAdapter);
    }

    private async Task ImportPackagesAsync(Interpreter interpreter, IReadOnlyList<string> packages)
    {
        if (packages == null || packages.Count == 0)
            return;
        
        foreach (var package in packages)
        {
            try
            {
                _Result? result = null;
                var script = $"package require {package}";
                
                var returnCode = await Task.Run(() => 
                    interpreter.EvaluateScript(script, ref result));
                
                if (returnCode != ReturnCode.Ok)
                {
                    _logger.LogWarning("Failed to import package {Package}: {Error}", 
                        package, result?.ToString());
                }
                else
                {
                    _logger.LogDebug("Successfully imported package {Package}", package);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error importing package {Package}", package);
            }
        }
    }
    
    private async Task SetWorkingDirectoryAsync(Interpreter interpreter, string workingDirectory)
    {
        if (string.IsNullOrEmpty(workingDirectory) || workingDirectory == Environment.CurrentDirectory)
            return;
        
        try
        {
            // Validate directory exists
            if (!Directory.Exists(workingDirectory))
            {
                _logger.LogWarning("Working directory does not exist: {Directory}", workingDirectory);
                return;
            }
            
            _Result? result = null;
            var script = $"cd \"{workingDirectory.Replace("\\", "/")}\"";
            
            var returnCode = await Task.Run(() => 
                interpreter.EvaluateScript(script, ref result));
            
            if (returnCode != ReturnCode.Ok)
            {
                _logger.LogWarning("Failed to set working directory to {Directory}: {Error}", 
                    workingDirectory, result?.ToString());
            }
            else
            {
                _logger.LogDebug("Set working directory to {Directory}", workingDirectory);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting working directory to {Directory}", workingDirectory);
        }
    }
    
    private async Task InjectEnvironmentVariablesAsync(
        Interpreter interpreter, 
        IReadOnlyDictionary<string, string> environmentVariables)
    {
        if (environmentVariables == null || environmentVariables.Count == 0)
            return;
        
        try
        {
            foreach (var kvp in environmentVariables)
            {
                _Result? result = null;
                // Set in the env array which Eagle/Tcl uses for environment variables
                // Escape the value to handle special characters
                var escapedValue = kvp.Value.Replace("\\", "\\\\")
                                           .Replace("\"", "\\\"")
                                           .Replace("$", "\\$")
                                           .Replace("[", "\\[")
                                           .Replace("]", "\\]");
                var script = $"set ::env({kvp.Key}) \"{escapedValue}\"";
                
                var returnCode = await Task.Run(() => 
                    interpreter.EvaluateScript(script, ref result));
                
                if (returnCode != ReturnCode.Ok)
                {
                    _logger.LogWarning("Failed to set environment variable {Name}: {Error}", 
                        kvp.Key, result?.ToString());
                }
                else
                {
                    _logger.LogDebug("Set environment variable {Name}", kvp.Key);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error injecting environment variables");
        }
    }

    private void ConfigureSecurityTracking(string executionId, EagleSecurityPolicy policy)
    {
        try
        {
            // Track security configuration
            // RecordSecurityCheck is an internal implementation detail not exposed through the interface
            // This is acceptable since both EagleScriptExecutor and EagleSecurityMonitor are in the Infrastructure layer
            if (_securityMonitor is EagleSecurityMonitor concreteMonitor)
            {
                concreteMonitor.RecordSecurityCheck(executionId, policy.Level, "interpreter_configured", true);
            }
            
            // Log security settings for this execution
            if (policy.AllowFileSystemAccess && policy.AllowedPaths?.Count > 0)
            {
                foreach (var path in policy.AllowedPaths)
                {
                    _logger.LogDebug("Execution {ExecutionId} allowed path: {Path}", executionId, path);
                }
            }
            
            if (policy.AllowClrReflection && policy.AllowedAssemblies?.Count > 0)
            {
                foreach (var assembly in policy.AllowedAssemblies)
                {
                    _logger.LogDebug("Execution {ExecutionId} allowed assembly: {Assembly}", executionId, assembly);
                }
            }
            
            if (policy.MaxExecutionTimeMs > 0)
            {
                _logger.LogDebug("Execution {ExecutionId} timeout: {Timeout}ms", executionId, policy.MaxExecutionTimeMs);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to configure security tracking for execution {ExecutionId}", executionId);
        }
    }


    private long GetCommandCount(Interpreter interpreter)
    {
        _Result? result = null;

        if (interpreter.EvaluateScript(
                "info cmdcount", ref result) != ReturnCode.Ok)
        {
            return Count.Invalid;
        }

        long count = 0;

        if (Value.GetWideInteger2(
                result, ValueFlags.AnyWideInteger,
                interpreter.CultureInfo, ref count) != ReturnCode.Ok)
        {
            return Count.Invalid;
        }

        return count;
    }

    private string? TryConvertToStructuredOutput(Interpreter interpreter, string result)
    {
        try
        {
            // First check if it's already valid JSON
            if ((result.StartsWith('{') && result.EndsWith('}')) || 
                (result.StartsWith('[') && result.EndsWith(']')))
            {
                try
                {
                    // Validate it's proper JSON
                    _ = System.Text.Json.JsonDocument.Parse(result);
                    return result;
                }
                catch
                {
                    // Not valid JSON, continue with list/dict detection
                }
            }

            // Get the Tcl dictionary converter
            var tclConverter = _serviceProvider.GetService<ITclDictionaryConverter>();
            if (tclConverter == null)
            {
                _logger.LogDebug("TclDictionaryConverter not available");
                return null;
            }

            // Try to detect if it's a Tcl dictionary
            if (tclConverter.IsTclDictionary(result))
            {
                _logger.LogDebug("Detected Tcl dictionary in output");
                try
                {
                    var jsonOutput = tclConverter.ConvertTclDictToJson(result);
                    _logger.LogDebug("Successfully converted Tcl dictionary to JSON");
                    return jsonOutput;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Failed to convert Tcl dictionary to JSON");
                }
            }

            // Try to detect if it's a Tcl list
            _Result? listCheckResult = null;
            var listCheckCode = interpreter.EvaluateScript($"llength {{{result}}}", ref listCheckResult);
            
            if (listCheckCode == ReturnCode.Ok && int.TryParse(listCheckResult?.ToString(), out int listLength) && listLength > 0)
            {
                _logger.LogDebug("Detected Tcl list with {Length} elements", listLength);
                
                // Check if it looks like a simple list (not already wrapped in braces)
                if (!result.Trim().StartsWith('{') || !result.Trim().EndsWith('}'))
                {
                    try
                    {
                        var jsonOutput = tclConverter.ConvertTclListToJson(result);
                        _logger.LogDebug("Successfully converted Tcl list to JSON");
                        return jsonOutput;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Failed to convert Tcl list to JSON");
                    }
                }
            }
            
            return null; // Not structured data or conversion failed
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to detect structured output");
            return null;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _interpreterPool?.Dispose();
        _executionSemaphore?.Dispose();
        // History store is managed by DI container
        _disposed = true;
    }
}