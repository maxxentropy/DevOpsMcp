namespace DevOpsMcp.Domain.Eagle;

/// <summary>
/// Performance and resource metrics for Eagle script execution
/// </summary>
public sealed record EagleExecutionMetrics
{
    /// <summary>
    /// Memory usage in bytes
    /// </summary>
    public long MemoryUsageBytes { get; init; }
    
    /// <summary>
    /// Number of Eagle commands executed
    /// </summary>
    public long CommandsExecuted { get; init; }
    
    /// <summary>
    /// Time spent compiling the script
    /// </summary>
    public TimeSpan CompilationTime { get; init; }
    
    /// <summary>
    /// Time spent executing the script (excluding compilation)
    /// </summary>
    public TimeSpan ExecutionTime { get; init; }
    
    /// <summary>
    /// Number of variables created during execution
    /// </summary>
    public int VariablesCreated { get; init; }
    
    /// <summary>
    /// Number of procedures defined
    /// </summary>
    public int ProceduresDefined { get; init; }
    
    /// <summary>
    /// Number of security checks performed
    /// </summary>
    public int SecurityChecksPerformed { get; init; }
    
    /// <summary>
    /// Custom metrics for extensibility
    /// </summary>
    public IReadOnlyDictionary<string, object> CustomMetrics { get; init; } = 
        new Dictionary<string, object>();
}