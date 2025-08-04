namespace DevOpsMcp.Domain.Eagle;

/// <summary>
/// Represents formatted output from Eagle script execution
/// </summary>
public class FormattedOutput
{
    /// <summary>
    /// The output format used
    /// </summary>
    public OutputFormat Format { get; set; }
    
    /// <summary>
    /// The formatted content
    /// </summary>
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// MIME content type
    /// </summary>
    public string ContentType { get; set; } = "text/plain";
    
    /// <summary>
    /// Indicates if the output is tabular data
    /// </summary>
    public bool IsTabular { get; set; }
    
    /// <summary>
    /// Error message if formatting failed
    /// </summary>
    public string? Error { get; set; }
    
    /// <summary>
    /// Metadata about the output
    /// </summary>
    public Dictionary<string, object> Metadata { get; } = new();
}