namespace DevOpsMcp.Domain.Eagle;

/// <summary>
/// Defines the complete context for Eagle script execution
/// </summary>
public sealed record ExecutionContext
{
    /// <summary>
    /// Correlation ID for distributed tracing
    /// </summary>
    public required Guid CorrelationId { get; init; }
    
    /// <summary>
    /// Optional session ID for stateful operations
    /// </summary>
    public string? SessionId { get; init; }
    
    /// <summary>
    /// Script to execute
    /// </summary>
    public required string Script { get; init; }
    
    /// <summary>
    /// Input variables to set before execution
    /// </summary>
    public IReadOnlyDictionary<string, object> Variables { get; init; } = 
        new Dictionary<string, object>();
    
    /// <summary>
    /// Eagle packages to import
    /// </summary>
    public IReadOnlyList<string> ImportedPackages { get; init; } = 
        Array.Empty<string>();
    
    /// <summary>
    /// Security policy for this execution
    /// </summary>
    public required EagleSecurityPolicy SecurityPolicy { get; init; }
    
    /// <summary>
    /// Maximum execution time
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);
    
    /// <summary>
    /// Working directory for the script
    /// </summary>
    public string WorkingDirectory { get; init; } = Environment.CurrentDirectory;
    
    /// <summary>
    /// Environment variables for the execution
    /// </summary>
    public IReadOnlyDictionary<string, string> EnvironmentVariables { get; init; } = 
        new Dictionary<string, string>();
    
    /// <summary>
    /// Maximum memory allowed in megabytes
    /// </summary>
    public int MaxMemoryMb { get; init; } = 256;
    
    /// <summary>
    /// Execution mode
    /// </summary>
    public ExecutionMode Mode { get; init; } = ExecutionMode.Batch;
}