using System.ComponentModel;

namespace DevOpsMcp.Server.Tools.Email;

/// <summary>
/// Arguments for sending templated email
/// </summary>
public sealed class SendTemplatedEmailToolArguments
{
    [Description("Recipient email address")]
    public required string To { get; init; }

    [Description("Name of the AWS SES email template to use")]
    public required string TemplateName { get; init; }

    [Description("Template data for variable substitution")]
    public Dictionary<string, object>? TemplateData { get; init; }

    [Description("CC recipients (optional)")]
    public List<string>? Cc { get; init; }

    [Description("BCC recipients (optional)")]
    public List<string>? Bcc { get; init; }
}