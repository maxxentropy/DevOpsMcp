namespace DevOpsMcp.Domain.Eagle;

/// <summary>
/// Security policy for Eagle script execution
/// </summary>
public sealed record EagleSecurityPolicy
{
    /// <summary>
    /// Security level preset
    /// </summary>
    public SecurityLevel Level { get; init; } = SecurityLevel.Standard;
    
    /// <summary>
    /// Allow file system access
    /// </summary>
    public bool AllowFileSystemAccess { get; init; }
    
    /// <summary>
    /// Allow network access
    /// </summary>
    public bool AllowNetworkAccess { get; init; }
    
    /// <summary>
    /// Allow CLR reflection and interop
    /// </summary>
    public bool AllowClrReflection { get; init; }
    
    /// <summary>
    /// Allow process execution
    /// </summary>
    public bool AllowProcessExecution { get; init; }
    
    /// <summary>
    /// Whitelisted .NET assemblies for reflection
    /// </summary>
    public IReadOnlyList<string> AllowedAssemblies { get; init; } = 
        Array.Empty<string>();
    
    /// <summary>
    /// Eagle commands to restrict/disable
    /// </summary>
    public IReadOnlyList<string> RestrictedCommands { get; init; } = 
        new[] { "exec", "socket", "open" };
    
    /// <summary>
    /// Allowed file system paths (if file access is enabled)
    /// </summary>
    public IReadOnlyList<string> AllowedPaths { get; init; } = 
        Array.Empty<string>();
    
    /// <summary>
    /// Maximum execution time in milliseconds
    /// </summary>
    public int MaxExecutionTimeMs { get; init; } = 30000;
    
    /// <summary>
    /// Maximum memory usage in megabytes
    /// </summary>
    public int MaxMemoryMb { get; init; } = 256;
    
    /// <summary>
    /// Allow access to environment variables
    /// </summary>
    public bool AllowEnvironmentAccess { get; init; }
    
    /// <summary>
    /// Create a minimal security policy (most restrictive)
    /// </summary>
    public static EagleSecurityPolicy Minimal => new()
    {
        Level = SecurityLevel.Minimal,
        AllowFileSystemAccess = false,
        AllowNetworkAccess = false,
        AllowClrReflection = false,
        AllowProcessExecution = false,
        AllowEnvironmentAccess = false,
        MaxExecutionTimeMs = 5000,
        MaxMemoryMb = 64
    };
    
    /// <summary>
    /// Create a standard security policy (balanced)
    /// </summary>
    public static EagleSecurityPolicy Standard => new()
    {
        Level = SecurityLevel.Standard,
        AllowFileSystemAccess = false,
        AllowNetworkAccess = false,
        AllowClrReflection = true,
        AllowProcessExecution = false,
        AllowEnvironmentAccess = true,
        AllowedAssemblies = new[] { "System", "System.Core", "System.Linq" },
        MaxExecutionTimeMs = 30000,
        MaxMemoryMb = 256
    };
}