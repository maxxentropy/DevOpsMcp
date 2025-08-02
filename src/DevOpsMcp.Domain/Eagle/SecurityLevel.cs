namespace DevOpsMcp.Domain.Eagle;

/// <summary>
/// Security level presets for Eagle execution
/// </summary>
public enum SecurityLevel
{
    /// <summary>
    /// Most restrictive - no external access
    /// </summary>
    Minimal,
    
    /// <summary>
    /// Balanced security - limited CLR access
    /// </summary>
    Standard,
    
    /// <summary>
    /// Less restrictive - broader CLR and file access
    /// </summary>
    Elevated,
    
    /// <summary>
    /// Least restrictive - full access (use with caution)
    /// </summary>
    Maximum
}