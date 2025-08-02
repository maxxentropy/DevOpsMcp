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
    /// Default security policy settings
    /// </summary>
    public SecurityPolicyOptions SecurityPolicy { get; set; } = new();
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