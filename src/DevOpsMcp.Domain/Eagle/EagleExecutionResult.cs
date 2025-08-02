namespace DevOpsMcp.Domain.Eagle;

/// <summary>
/// Represents the result of an Eagle script execution
/// </summary>
public sealed record EagleExecutionResult
{
    /// <summary>
    /// Unique identifier for this execution
    /// </summary>
    public required string ExecutionId { get; init; }
    
    /// <summary>
    /// Indicates whether the execution completed successfully
    /// </summary>
    public required bool IsSuccess { get; init; }
    
    /// <summary>
    /// The script output or result value
    /// </summary>
    public string? Result { get; init; }
    
    /// <summary>
    /// Error message if execution failed
    /// </summary>
    public string? ErrorMessage { get; init; }
    
    /// <summary>
    /// Time when execution started
    /// </summary>
    public required DateTime StartTimeUtc { get; init; }
    
    /// <summary>
    /// Time when execution completed
    /// </summary>
    public required DateTime EndTimeUtc { get; init; }
    
    /// <summary>
    /// Execution metrics and performance data
    /// </summary>
    public required EagleExecutionMetrics Metrics { get; init; }
    
    /// <summary>
    /// Exit code from the script execution
    /// </summary>
    public int ExitCode { get; init; }
    
    /// <summary>
    /// Calculated duration of the execution
    /// </summary>
    public TimeSpan Duration => EndTimeUtc - StartTimeUtc;
    
    /// <summary>
    /// Security policy violations encountered during execution
    /// </summary>
    public IReadOnlyList<string> SecurityViolations { get; init; } = Array.Empty<string>();
}