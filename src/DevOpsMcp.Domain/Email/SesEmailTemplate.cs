namespace DevOpsMcp.Domain.Email;

/// <summary>
/// Represents an AWS SES email template
/// </summary>
public sealed class SesEmailTemplate
{
    /// <summary>
    /// Unique name of the template
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Email subject line with variable placeholders
    /// </summary>
    public string Subject { get; set; } = string.Empty;

    /// <summary>
    /// HTML content with variable placeholders
    /// </summary>
    public string HtmlContent { get; set; } = string.Empty;

    /// <summary>
    /// Plain text content with variable placeholders
    /// </summary>
    public string TextContent { get; set; } = string.Empty;

    /// <summary>
    /// When the template was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the template was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Template version number
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Whether the template is active
    /// </summary>
    public bool IsActive { get; set; } = true;
}