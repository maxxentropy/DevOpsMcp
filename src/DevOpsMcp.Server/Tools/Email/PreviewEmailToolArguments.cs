using System.ComponentModel;

namespace DevOpsMcp.Server.Tools.Email;

/// <summary>
/// Arguments for the preview email tool
/// </summary>
public sealed class PreviewEmailToolArguments
{
    [Description("Name of the email template to preview")]
    public required string TemplateName { get; init; }

    [Description("Data to pass to the template")]
    public required Dictionary<string, object> TemplateData { get; init; }

    [Description("Output format: html, text, both (default: both)")]
    public string? Format { get; init; }
}