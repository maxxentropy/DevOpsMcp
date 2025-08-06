using System.Text.Json;
using DevOpsMcp.Domain.Interfaces;
using DevOpsMcp.Server.Mcp;

namespace DevOpsMcp.Server.Tools.Email;

/// <summary>
/// MCP tool for sending templated emails using AWS SES templates
/// </summary>
public sealed class SendTemplatedEmailTool(IEmailService emailService) : BaseTool<SendTemplatedEmailToolArguments>
{
    public override string Name => "send_templated_email";
    
    public override string Description => 
        "Send an email using an AWS SES template with variable substitution.";
    
    public override JsonElement InputSchema => CreateSchema<SendTemplatedEmailToolArguments>();

    protected override async Task<CallToolResponse> ExecuteInternalAsync(
        SendTemplatedEmailToolArguments arguments, 
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await emailService.SendTemplatedEmailAsync(
                toAddress: arguments.To,
                templateName: arguments.TemplateName,
                templateData: arguments.TemplateData ?? new Dictionary<string, object>(),
                cc: arguments.Cc,
                bcc: arguments.Bcc,
                cancellationToken: cancellationToken);

            if (result.IsError)
            {
                return CreateErrorResponse($"Failed to send templated email: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            return CreateJsonResponse(new
            {
                success = true,
                messageId = result.Value.MessageId,
                to = arguments.To,
                templateName = arguments.TemplateName,
                timestamp = result.Value.Timestamp
            });
        }
        catch (Exception ex)
        {
            return CreateErrorResponse($"Unexpected error: {ex.Message}");
        }
    }
}