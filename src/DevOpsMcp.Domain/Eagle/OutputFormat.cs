namespace DevOpsMcp.Domain.Eagle;

/// <summary>
/// Supported output formats for Eagle script execution results
/// </summary>
public enum OutputFormat
{
    /// <summary>
    /// Plain text output
    /// </summary>
    Plain,
    
    /// <summary>
    /// JSON formatted output
    /// </summary>
    Json,
    
    /// <summary>
    /// XML formatted output
    /// </summary>
    Xml,
    
    /// <summary>
    /// YAML formatted output
    /// </summary>
    Yaml,
    
    /// <summary>
    /// Table formatted output
    /// </summary>
    Table,
    
    /// <summary>
    /// CSV formatted output
    /// </summary>
    Csv,
    
    /// <summary>
    /// Markdown formatted output
    /// </summary>
    Markdown
}