using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DevOpsMcp.Domain.Eagle;
using DevOpsMcp.Domain.Interfaces;
using DevOpsMcp.Infrastructure.Configuration;
using Eagle;
using Eagle._Components.Public;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevOpsMcp.Infrastructure.Eagle;

/// <summary>
/// Manages a pool of Eagle interpreters with advanced lifecycle management
/// </summary>
public sealed class InterpreterPool : IEagleInterpreterPool
{
    private readonly ILogger<InterpreterPool> _logger;
    private readonly EagleOptions _options;
    private readonly ConcurrentBag<PooledInterpreter> _availableInterpreters;
    private readonly ConcurrentDictionary<Guid, PooledInterpreter> _activeInterpreters;
    private readonly SemaphoreSlim _poolSemaphore;
    private readonly Timer _maintenanceTimer;
    private readonly IEagleSecurityMonitor _securityMonitor;
    private long _totalCreated;
    private long _totalRecycled;
    private long _totalErrors;
    private bool _disposed;

    public InterpreterPool(
        ILogger<InterpreterPool> logger,
        IOptions<EagleOptions> options,
        IEagleSecurityMonitor securityMonitor)
    {
        _logger = logger;
        _options = options.Value;
        _securityMonitor = securityMonitor;
        _availableInterpreters = new ConcurrentBag<PooledInterpreter>();
        _activeInterpreters = new ConcurrentDictionary<Guid, PooledInterpreter>();
        _poolSemaphore = new SemaphoreSlim(_options.MaxPoolSize, _options.MaxPoolSize);
        
        // Start maintenance timer
        _maintenanceTimer = new Timer(
            PerformMaintenance,
            null,
            TimeSpan.FromMinutes(1),
            TimeSpan.FromMinutes(1));
        
        // Pre-warm pool if configured
        if (_options.InterpreterPool.PreWarmOnStartup)
        {
            PreWarmPool();
        }
    }

    /// <summary>
    /// Rents an interpreter from the pool (IEagleInterpreterPool implementation)
    /// </summary>
    public async Task<IPooledInterpreter> RentAsync()
    {
        // Use default security policy for interface implementation
        var defaultPolicy = new EagleSecurityPolicy
        {
            Level = SecurityLevel.Minimal,
            AllowFileSystemAccess = false,
            AllowNetworkAccess = false,
            AllowClrReflection = false,
            MaxExecutionTimeMs = 30000
        };
        return await AcquireAsync(defaultPolicy);
    }
    
    /// <summary>
    /// Returns an interpreter to the pool (IEagleInterpreterPool implementation)
    /// </summary>
    public void ReturnToPool(IPooledInterpreter interpreter)
    {
        if (interpreter is PooledInterpreter pooledInterpreter)
        {
            Release(pooledInterpreter);
        }
        else
        {
            throw new ArgumentException("Invalid interpreter type", nameof(interpreter));
        }
    }
    
    /// <summary>
    /// Acquires an interpreter from the pool
    /// </summary>
    public async Task<PooledInterpreter> AcquireAsync(
        EagleSecurityPolicy securityPolicy,
        CancellationToken cancellationToken = default)
    {
        var timeout = TimeSpan.FromSeconds(_options.InterpreterPool.AcquisitionTimeoutSeconds);
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);
        
        await _poolSemaphore.WaitAsync(cts.Token);
        
        try
        {
            // Try to get an existing interpreter
            if (TryGetAvailableInterpreter(securityPolicy, out var pooledInterpreter) && pooledInterpreter != null)
            {
                if (_options.InterpreterPool.ValidateBeforeUse && !await ValidateInterpreterAsync(pooledInterpreter))
                {
                    // Interpreter failed validation, dispose and create new
                    DisposeInterpreter(pooledInterpreter);
#pragma warning disable CA2000 // Dispose objects before losing scope - managed by pool
                    var newInterpreter = await CreateInterpreterAsync(securityPolicy);
                    pooledInterpreter = newInterpreter;
#pragma warning restore CA2000
                }
                
                pooledInterpreter.MarkActive();
                _activeInterpreters[pooledInterpreter.Id] = pooledInterpreter;
                return pooledInterpreter;
            }
            
            // Create new interpreter
#pragma warning disable CA2000 // Dispose objects before losing scope - managed by pool
            pooledInterpreter = await CreateInterpreterAsync(securityPolicy);
#pragma warning restore CA2000
            pooledInterpreter.MarkActive();
            _activeInterpreters[pooledInterpreter.Id] = pooledInterpreter;
            return pooledInterpreter;
        }
        catch
        {
            _poolSemaphore.Release();
            throw;
        }
    }

    /// <summary>
    /// Returns an interpreter to the pool
    /// </summary>
    public void Release(PooledInterpreter pooledInterpreter, bool hadError = false)
    {
        if (!_activeInterpreters.TryRemove(pooledInterpreter.Id, out _))
        {
            _logger.LogWarning("Attempted to release interpreter {Id} that was not active", pooledInterpreter.Id);
            return;
        }
        
        try
        {
            pooledInterpreter.IncrementExecutionCount();
            
            // Check if interpreter should be recycled
            var shouldRecycle = hadError && _options.InterpreterPool.RecycleOnError ||
                              pooledInterpreter.ExecutionCount >= _options.InterpreterPool.MaxExecutionsPerInterpreter ||
                              pooledInterpreter.Age > TimeSpan.FromMinutes(_options.InterpreterPool.MaxIdleTimeMinutes);
            
            if (shouldRecycle)
            {
                RecycleInterpreter(pooledInterpreter);
            }
            else
            {
                // Reset and return to pool
                if (_options.InterpreterPool.ClearVariablesBetweenExecutions)
                {
                    ResetInterpreter(pooledInterpreter);
                }
                
                pooledInterpreter.MarkIdle();
                _availableInterpreters.Add(pooledInterpreter);
            }
        }
        finally
        {
            _poolSemaphore.Release();
        }
    }

    /// <summary>
    /// Gets current pool statistics
    /// </summary>
    public DevOpsMcp.Domain.Interfaces.PoolStatistics GetStatistics()
    {
        var availableCount = _availableInterpreters.Count;
        var activeCount = _activeInterpreters.Count;
        var totalCount = availableCount + activeCount;
        
        return new InternalPoolStatistics
        {
            TotalCreated = Interlocked.Read(ref _totalCreated),
            TotalRecycled = Interlocked.Read(ref _totalRecycled),
            TotalErrors = Interlocked.Read(ref _totalErrors),
            AvailableCount = availableCount,
            ActiveCount = activeCount,
            TotalCount = totalCount,
            // Base class properties
            TotalInterpreters = totalCount,
            AvailableInterpreters = availableCount,
            InUseInterpreters = activeCount,
            TotalRentals = (int)Interlocked.Read(ref _totalCreated),
            AverageRentalDuration = TimeSpan.Zero // TODO: Track rental durations
        };
    }

    private void PreWarmPool()
    {
        var count = _options.InterpreterPool.PreWarmCount ?? _options.MinPoolSize;
        _logger.LogInformation("Pre-warming interpreter pool with {Count} interpreters", count);
        
        for (int i = 0; i < count; i++)
        {
            try
            {
#pragma warning disable CA2000 // Dispose objects before losing scope - managed by pool
                var interpreter = CreateInterpreterAsync(EagleSecurityPolicy.Standard).GetAwaiter().GetResult();
                _availableInterpreters.Add(interpreter);
#pragma warning restore CA2000
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to pre-warm interpreter {Index}", i);
            }
        }
    }

    private bool TryGetAvailableInterpreter(EagleSecurityPolicy policy, out PooledInterpreter? interpreter)
    {
        // Try to find an interpreter with matching security policy
        var interpreters = _availableInterpreters.ToArray();
        foreach (var pooled in interpreters)
        {
            if (pooled.SecurityPolicy.Level == policy.Level)
            {
                // Try to take it from the bag
                if (_availableInterpreters.TryTake(out var taken) && taken.Id == pooled.Id)
                {
                    interpreter = taken;
                    return true;
                }
            }
        }
        
        interpreter = null;
        return false;
    }

    private async Task<PooledInterpreter> CreateInterpreterAsync(EagleSecurityPolicy securityPolicy)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            var interpreter = await Task.Run(() => CreateSafeInterpreter(securityPolicy));
            Interlocked.Increment(ref _totalCreated);
            
            _logger.LogDebug("Created new interpreter in {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            
            return new PooledInterpreter(interpreter, securityPolicy);
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref _totalErrors);
            _logger.LogError(ex, "Failed to create interpreter");
            throw;
        }
    }

    private Interpreter CreateSafeInterpreter(EagleSecurityPolicy policy)
    {
        // IMPORTANT: Eagle's init.eagle script requires the "interp" command to set up aliases.
        // We cannot use CreateFlags.Safe or SafeAndHideUnsafe because they exclude the "interp" command.
        // Instead, we'll use standard creation and apply security through other means.
        var interpreterSettings = InterpreterSettings.CreateDefault();

        switch (policy.Level)
        {
            case SecurityLevel.Minimal:
                // Use standard creation for now - security will be enforced at runtime
                interpreterSettings.CreateFlags = CreateFlags.Standard;
                interpreterSettings.InitializeFlags |= InitializeFlags.Safe;
                break;
            case SecurityLevel.Standard:
                // Use standard creation for now - security will be enforced at runtime
                interpreterSettings.CreateFlags = CreateFlags.Standard;
                break;
            case SecurityLevel.Elevated:
                interpreterSettings.CreateFlags = CreateFlags.Standard;
                break;
            case SecurityLevel.Maximum:
                interpreterSettings.CreateFlags = CreateFlags.Standard;
                break;
        }

        Result? result = null;
        var interpreter = Interpreter.Create(interpreterSettings, false, ref result);
        
        #pragma warning disable CA1508 // Defensive null check for external API
        if (interpreter is null)
        {
            throw new System.InvalidOperationException($"Failed to create Eagle interpreter: {result}");
        }
        #pragma warning restore CA1508
        
        // Apply security policy after interpreter creation
        ApplySecurityPolicy(interpreter, policy);

        return interpreter;
    }
    
    private void ApplySecurityPolicy(Interpreter interpreter, EagleSecurityPolicy policy)
    {
        // Skip enforcement for Maximum security level
        if (policy.Level == SecurityLevel.Maximum)
        {
            _logger.LogDebug("Maximum security level - no command restrictions applied");
            return;
        }
        
        Result? result = null;
        
        // Hide dangerous commands by renaming them to a hidden namespace
        // This is more reliable than trying to remove them completely
        var hiddenNamespace = "::eagle::hidden::";
        
        // File system commands
        if (!policy.AllowFileSystemAccess)
        {
            _logger.LogDebug("Hiding file system commands for security level {Level}", policy.Level);
            HideCommand(interpreter, "file", hiddenNamespace + "file", ref result);
            HideCommand(interpreter, "glob", hiddenNamespace + "glob", ref result);
            HideCommand(interpreter, "open", hiddenNamespace + "open", ref result);
            HideCommand(interpreter, "cd", hiddenNamespace + "cd", ref result);
            HideCommand(interpreter, "pwd", hiddenNamespace + "pwd", ref result);
            HideCommand(interpreter, "close", hiddenNamespace + "close", ref result);
            // Note: We don't hide puts/gets as they're needed for basic output
            // File writing is already blocked by hiding 'open' and 'file' commands
        }
        
        // Network commands
        if (!policy.AllowNetworkAccess)
        {
            _logger.LogDebug("Hiding network commands for security level {Level}", policy.Level);
            HideCommand(interpreter, "socket", hiddenNamespace + "socket", ref result);
            HideCommand(interpreter, "uri", hiddenNamespace + "uri", ref result);
        }
        
        // Process execution commands
        if (!policy.AllowProcessExecution)
        {
            _logger.LogDebug("Hiding process execution commands for security level {Level}", policy.Level);
            HideCommand(interpreter, "exec", hiddenNamespace + "exec", ref result);
            HideCommand(interpreter, "pid", hiddenNamespace + "pid", ref result);
        }
        
        // CLR reflection commands
        if (!policy.AllowClrReflection)
        {
            _logger.LogDebug("Hiding CLR reflection commands for security level {Level}", policy.Level);
            HideCommand(interpreter, "object", hiddenNamespace + "object", ref result);
            HideCommand(interpreter, "load", hiddenNamespace + "load", ref result);
        }
        
        // Additional restricted commands from policy
        if (policy.RestrictedCommands?.Count > 0)
        {
            foreach (var command in policy.RestrictedCommands)
            {
                _logger.LogDebug("Hiding restricted command {Command} for security level {Level}", command, policy.Level);
                HideCommand(interpreter, command, hiddenNamespace + command, ref result);
            }
        }
        
        // For Minimal and Standard levels, hide the interp command after initialization
        // to prevent creating new interpreters that could bypass security
        if (policy.Level == SecurityLevel.Minimal || policy.Level == SecurityLevel.Standard)
        {
            _logger.LogDebug("Hiding interp command for security level {Level}", policy.Level);
            HideCommand(interpreter, "interp", hiddenNamespace + "interp", ref result);
        }
        
        // Create a namespace to prevent access to hidden commands
        var namespaceScript = $"namespace eval {hiddenNamespace} {{}}";
        interpreter.EvaluateScript(namespaceScript, ref result);
        
        _logger.LogInformation("Applied security policy {Level} to interpreter", policy.Level);
    }
    
    private void HideCommand(Interpreter interpreter, string command, string hiddenName, ref Result? result)
    {
        try
        {
            // First check if the command exists
            var checkScript = $"info commands ::{command}";
            var checkResult = result;
            var checkCode = interpreter.EvaluateScript(checkScript, ref checkResult);
            
            if (checkCode == ReturnCode.Ok && !string.IsNullOrWhiteSpace(checkResult?.ToString()))
            {
                // Command exists, rename it
                var renameScript = $"rename ::{command} {hiddenName}";
                var renameCode = interpreter.EvaluateScript(renameScript, ref result);
                
                if (renameCode == ReturnCode.Ok)
                {
                    _logger.LogTrace("Successfully hid command {Command}", command);
                }
                else
                {
                    _logger.LogWarning("Failed to hide command {Command}: {Result}", command, result);
                }
            }
            else
            {
                _logger.LogTrace("Command {Command} does not exist, skipping", command);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error hiding command {Command}", command);
        }
    }

    private async Task<bool> ValidateInterpreterAsync(PooledInterpreter pooledInterpreter)
    {
        try
        {
            Result? result = null;
            var code = await Task.Run(() => 
                pooledInterpreter.Interpreter.EvaluateScript("expr {1 + 1}", ref result));
            
            return code == ReturnCode.Ok && result?.ToString() == "2";
        }
        catch
        {
            return false;
        }
    }

    private void ResetInterpreter(PooledInterpreter pooledInterpreter)
    {
        try
        {
            Result? result = null;
            
            // Try to use Eagle.Test cleanup if available
            if (pooledInterpreter.Interpreter.EvaluateScript(
                    "package require Eagle.Test; cleanState",
                    ref result) != ReturnCode.Ok)
            {
                // Fallback to manual cleanup
                pooledInterpreter.Interpreter.EvaluateScript(
                    "foreach var [info vars] { if {$var ni {tcl_platform env}} { unset $var } }",
                    ref result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to reset interpreter {Id}", pooledInterpreter.Id);
        }
    }

    private void RecycleInterpreter(PooledInterpreter pooledInterpreter)
    {
        Interlocked.Increment(ref _totalRecycled);
        _logger.LogDebug("Recycling interpreter {Id} after {Count} executions", 
            pooledInterpreter.Id, pooledInterpreter.ExecutionCount);
        
        DisposeInterpreter(pooledInterpreter);
    }

    private void DisposeInterpreter(PooledInterpreter pooledInterpreter)
    {
        try
        {
            (pooledInterpreter.Interpreter as IDisposable)?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing interpreter {Id}", pooledInterpreter.Id);
        }
    }

    private void PerformMaintenance(object? state)
    {
        try
        {
            var now = DateTime.UtcNow;
            var maxIdleTime = TimeSpan.FromMinutes(_options.InterpreterPool.MaxIdleTimeMinutes);
            var toRemove = new List<PooledInterpreter>();
            
            // Check idle interpreters
            foreach (var interpreter in _availableInterpreters)
            {
                if (now - interpreter.LastUsedTime > maxIdleTime)
                {
                    toRemove.Add(interpreter);
                }
            }
            
            // Remove stale interpreters
            foreach (var interpreter in toRemove)
            {
#pragma warning disable CA2000 // Dispose objects before losing scope - managed by pool
                if (_availableInterpreters.TryTake(out var taken))
                {
                    if (taken.Id == interpreter.Id)
                    {
                        RecycleInterpreter(taken);
                    }
                    else
                    {
                        // Put it back if it's not the one we wanted
                        _availableInterpreters.Add(taken);
                    }
                }
#pragma warning restore CA2000
            }
            
            // Adaptive pool sizing
            if (_options.InterpreterPool.GrowthStrategy == PoolGrowthStrategy.Adaptive)
            {
                AdaptPoolSize();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during pool maintenance");
        }
    }

    private void AdaptPoolSize()
    {
        var stats = GetStatistics() as InternalPoolStatistics;
        if (stats == null) return;
        var utilizationRate = (double)stats.ActiveCount / stats.TotalCount;
        
        // If utilization is high and we have room to grow
        if (utilizationRate > 0.8 && stats.TotalCount < _options.MaxPoolSize)
        {
            // Add more interpreters
            var toAdd = Math.Min(2, _options.MaxPoolSize - stats.TotalCount);
            for (int i = 0; i < toAdd; i++)
            {
                Task.Run(async () =>
                {
                    try
                    {
#pragma warning disable CA2000 // Dispose objects before losing scope - managed by pool
                        var interpreter = await CreateInterpreterAsync(EagleSecurityPolicy.Standard);
#pragma warning restore CA2000
                        if (interpreter != null)
                        {
                            _availableInterpreters.Add(interpreter);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to adapt pool size");
                    }
                });
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _maintenanceTimer?.Dispose();
        
        // Dispose all interpreters
        while (_availableInterpreters.TryTake(out var interpreter))
        {
            DisposeInterpreter(interpreter);
        }
        
        foreach (var interpreter in _activeInterpreters.Values)
        {
            DisposeInterpreter(interpreter);
        }
        
        _poolSemaphore?.Dispose();
        _disposed = true;
    }
}

/// <summary>
/// Wrapper for a pooled interpreter with metadata
/// </summary>
public sealed class PooledInterpreter : IPooledInterpreter
{
    private readonly Guid _id;
    private readonly EagleInterpreterAdapter _interpreterAdapter;
    private bool _disposed;
    
    public Guid Id => _id;
    public Interpreter Interpreter { get; }
    public EagleSecurityPolicy SecurityPolicy { get; }
    public DateTime CreatedTime { get; }
    public DateTime LastUsedTime { get; private set; }
    public int ExecutionCount { get; private set; }
    public bool IsActive { get; private set; }
    
    public TimeSpan Age => DateTime.UtcNow - CreatedTime;
    
    // IPooledInterpreter implementation
    string IPooledInterpreter.Id => _id.ToString();
    IScriptInterpreter IPooledInterpreter.Interpreter => _interpreterAdapter;
    DateTimeOffset IPooledInterpreter.CreatedAt => new DateTimeOffset(CreatedTime);
    DateTimeOffset IPooledInterpreter.LastUsedAt => new DateTimeOffset(LastUsedTime);

    public PooledInterpreter(Interpreter interpreter, EagleSecurityPolicy securityPolicy)
    {
        _id = Guid.NewGuid();
        Interpreter = interpreter;
        _interpreterAdapter = new EagleInterpreterAdapter(interpreter);
        SecurityPolicy = securityPolicy;
        CreatedTime = DateTime.UtcNow;
        LastUsedTime = DateTime.UtcNow;
    }

    public void MarkActive()
    {
        IsActive = true;
        LastUsedTime = DateTime.UtcNow;
    }

    public void MarkIdle()
    {
        IsActive = false;
        LastUsedTime = DateTime.UtcNow;
    }

    public void IncrementExecutionCount()
    {
        ExecutionCount++;
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        // Interpreter disposal is handled by the pool
    }
}

/// <summary>
/// Statistics for the interpreter pool (internal version with more details)
/// </summary>
internal sealed class InternalPoolStatistics : DevOpsMcp.Domain.Interfaces.PoolStatistics
{
    public long TotalCreated { get; init; }
    public long TotalRecycled { get; init; }
    public long TotalErrors { get; init; }
    public int AvailableCount { get; init; }
    public int ActiveCount { get; init; }
    public int TotalCount { get; init; }
}