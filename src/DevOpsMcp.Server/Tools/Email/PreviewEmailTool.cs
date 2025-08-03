using System.Text.Json;
using DevOpsMcp.Domain.Interfaces;
using DevOpsMcp.Server.Mcp;

namespace DevOpsMcp.Server.Tools.Email;

/// <summary>
/// MCP tool for previewing email templates without sending
/// </summary>
public sealed class PreviewEmailTool(IEmailTemplateRenderer templateRenderer) : BaseTool<PreviewEmailToolArguments>
{
    public override string Name => "preview_email";
    
    public override string Description => 
        "Preview an email template with sample data without sending";
    
    public override JsonElement InputSchema => CreateSchema<PreviewEmailToolArguments>();

    protected override async Task<CallToolResponse> ExecuteInternalAsync(
        PreviewEmailToolArguments arguments, 
        CancellationToken cancellationToken)
    {
        var format = arguments.Format?.ToLowerInvariant() ?? "both";

        // Render template
        var result = await templateRenderer.RenderAsync(arguments.TemplateName, arguments.TemplateData, cancellationToken);

        if (result.IsError)
        {
            return CreateErrorResponse(
                $"Failed to render template: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        var rendered = result.Value;
        var response = new Dictionary<string, object>
        {
            ["templateName"] = rendered.TemplateName,
            ["renderDuration"] = rendered.RenderDuration.TotalMilliseconds,
            ["fromCache"] = rendered.FromCache
        };

        // Add content based on format
        switch (format)
        {
            case "html":
                response["html"] = rendered.InlinedHtmlContent;
                break;
            case "text":
                response["text"] = rendered.TextContent;
                break;
            default: // both
                response["html"] = rendered.InlinedHtmlContent;
                response["text"] = rendered.TextContent;
                break;
        }

        return CreateJsonResponse(response);
    }
}