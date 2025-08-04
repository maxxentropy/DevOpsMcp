namespace DevOpsMcp.Infrastructure.Configuration;

/// <summary>
/// Configuration options for Eagle script execution
/// </summary>
public sealed class EagleOptions
{
    public const string SectionName = "Eagle";
    
    /// <summary>
    /// Maximum number of concurrent script executions
    /// </summary>
    public int MaxConcurrentExecutions { get; set; } = 10;
    
    /// <summary>
    /// Minimum number of interpreters to keep in pool
    /// </summary>
    public int MinPoolSize { get; set; } = 2;
    
    /// <summary>
    /// Maximum number of interpreters in pool
    /// </summary>
    public int MaxPoolSize { get; set; } = 10;
    
    /// <summary>
    /// Interpreter pool configuration
    /// </summary>
    public InterpreterPoolOptions InterpreterPool { get; set; } = new();
    
    /// <summary>
    /// Default security policy settings
    /// </summary>
    public SecurityPolicyOptions SecurityPolicy { get; set; } = new();
    
    /// <summary>
    /// Session store configuration
    /// </summary>
    public SessionStoreOptions SessionStore { get; set; } = new();
}

/// <summary>
/// Configuration options for interpreter pooling
/// </summary>
public sealed class InterpreterPoolOptions
{
    /// <summary>
    /// Whether to pre-warm the pool on startup
    /// </summary>
    public bool PreWarmOnStartup { get; set; } = true;
    
    /// <summary>
    /// Number of interpreters to pre-warm (defaults to MinPoolSize)
    /// </summary>
    public int? PreWarmCount { get; set; }
    
    /// <summary>
    /// Maximum idle time before an interpreter is recycled (in minutes)
    /// </summary>
    public int MaxIdleTimeMinutes { get; set; } = 30;
    
    /// <summary>
    /// Maximum number of scripts an interpreter can execute before recycling
    /// </summary>
    public int MaxExecutionsPerInterpreter { get; set; } = 100;
    
    /// <summary>
    /// Whether to recycle interpreters after errors
    /// </summary>
    public bool RecycleOnError { get; set; } = true;
    
    /// <summary>
    /// Timeout for acquiring an interpreter from the pool (in seconds)
    /// </summary>
    public int AcquisitionTimeoutSeconds { get; set; } = 30;
    
    /// <summary>
    /// Whether to validate interpreter health before use
    /// </summary>
    public bool ValidateBeforeUse { get; set; } = true;
    
    /// <summary>
    /// Whether to clear variables between executions
    /// </summary>
    public bool ClearVariablesBetweenExecutions { get; set; } = true;
    
    /// <summary>
    /// Strategy for pool growth (Eager, Lazy, Adaptive)
    /// </summary>
    public PoolGrowthStrategy GrowthStrategy { get; set; } = PoolGrowthStrategy.Lazy;
}

/// <summary>
/// Strategies for interpreter pool growth
/// </summary>
public enum PoolGrowthStrategy
{
    /// <summary>
    /// Create interpreters eagerly up to max pool size
    /// </summary>
    Eager,
    
    /// <summary>
    /// Create interpreters only when needed
    /// </summary>
    Lazy,
    
    /// <summary>
    /// Adapt pool size based on usage patterns
    /// </summary>
    Adaptive
}

/// <summary>
/// Security policy configuration
/// </summary>
public sealed class SecurityPolicyOptions
{
    /// <summary>
    /// Default security level
    /// </summary>
    public string DefaultLevel { get; set; } = "Standard";
    
    /// <summary>
    /// Allow file system access by default
    /// </summary>
    public bool AllowFileSystemAccess { get; set; }
    
    /// <summary>
    /// Allow network access by default
    /// </summary>
    public bool AllowNetworkAccess { get; set; }
    
    /// <summary>
    /// Allow CLR reflection by default
    /// </summary>
    public bool AllowClrReflection { get; set; }
    
    /// <summary>
    /// Allowed assemblies for CLR reflection
    /// </summary>
    public List<string> AllowedAssemblies { get; init; } = new();
    
    /// <summary>
    /// Commands to restrict by default
    /// </summary>
    public List<string> RestrictedCommands { get; init; } = new() { "exec", "socket" };
    
    /// <summary>
    /// Maximum execution time in seconds
    /// </summary>
    public int MaxExecutionTimeSeconds { get; set; } = 30;
    
    /// <summary>
    /// Maximum memory in MB
    /// </summary>
    public int MaxMemoryMb { get; set; } = 256;
}

/// <summary>
/// Configuration options for session store
/// </summary>
public sealed class SessionStoreOptions
{
    /// <summary>
    /// Path to SQLite database file
    /// </summary>
    public string? DatabasePath { get; set; }
    
    /// <summary>
    /// Maximum age for session data before cleanup (in hours)
    /// </summary>
    public int MaxAgeHours { get; set; } = 24;
    
    /// <summary>
    /// Enable automatic cleanup of old sessions
    /// </summary>
    public bool EnableAutoCleanup { get; set; } = true;
    
    /// <summary>
    /// Cleanup interval in minutes
    /// </summary>
    public int CleanupIntervalMinutes { get; set; } = 60;
}