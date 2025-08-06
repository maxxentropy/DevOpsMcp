using System.ComponentModel;

namespace DevOpsMcp.Server.Tools.Email;

/// <summary>
/// Arguments for sending team email
/// </summary>
public sealed class SendTeamEmailToolArguments
{
    [Description("Email subject line")]
    public required string Subject { get; init; }

    [Description("Email body content (HTML or plain text)")]
    public required string Body { get; init; }

    [Description("Whether the body is HTML content (default: true)")]
    public bool? IsHtml { get; init; }
}