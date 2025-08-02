namespace DevOpsMcp.Domain.Eagle;

/// <summary>
/// Execution mode for Eagle scripts
/// </summary>
public enum ExecutionMode
{
    /// <summary>
    /// Interactive execution with immediate feedback
    /// </summary>
    Interactive,
    
    /// <summary>
    /// Batch execution mode
    /// </summary>
    Batch,
    
    /// <summary>
    /// Service mode for long-running scripts
    /// </summary>
    Service,
    
    /// <summary>
    /// Debug mode with additional diagnostics
    /// </summary>
    Debug
}