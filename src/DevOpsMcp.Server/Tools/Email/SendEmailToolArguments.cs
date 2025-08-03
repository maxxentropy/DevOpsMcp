using System.ComponentModel;

namespace DevOpsMcp.Server.Tools.Email;

/// <summary>
/// Arguments for the send email tool
/// </summary>
public sealed class SendEmailToolArguments
{
    [Description("Recipient email address")]
    public required string To { get; init; }

    [Description("CC recipients (optional)")]
    public List<string>? Cc { get; init; }

    [Description("BCC recipients (optional)")]
    public List<string>? Bcc { get; init; }

    [Description("Email subject")]
    public required string Subject { get; init; }

    [Description("Name of the email template to use")]
    public required string TemplateName { get; init; }

    [Description("Data to pass to the template")]
    public required Dictionary<string, object> TemplateData { get; init; }

    [Description("Reply-to address (optional)")]
    public string? ReplyTo { get; init; }

    [Description("Tags for tracking (optional)")]
    public Dictionary<string, string>? Tags { get; init; }

    [Description("Email priority: Low, Normal, High (default: Normal)")]
    public string? Priority { get; init; }

    [Description("Security policy override: Development, Standard, Restricted (optional)")]
    public string? SecurityPolicy { get; init; }
}