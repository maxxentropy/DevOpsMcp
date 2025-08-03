using System.Text.Json;
using DevOpsMcp.Application.Email.Commands;
using DevOpsMcp.Domain.Email;
using DevOpsMcp.Server.Mcp;
using MediatR;

namespace DevOpsMcp.Server.Tools.Email;

/// <summary>
/// MCP tool for sending emails through AWS SES
/// </summary>
public sealed class SendEmailTool(IMediator mediator) : BaseTool<SendEmailToolArguments>
{
    public override string Name => "send_email";
    
    public override string Description => 
        "Send an email using AWS SES with template rendering";
    
    public override JsonElement InputSchema => CreateSchema<SendEmailToolArguments>();

    protected override async Task<CallToolResponse> ExecuteInternalAsync(
        SendEmailToolArguments arguments, 
        CancellationToken cancellationToken)
    {
        var command = new SendEmailCommand
        {
            To = arguments.To,
            Subject = arguments.Subject,
            TemplateName = arguments.TemplateName,
            TemplateData = arguments.TemplateData,
            Cc = arguments.Cc ?? new List<string>(),
            Bcc = arguments.Bcc ?? new List<string>(),
            ReplyTo = arguments.ReplyTo,
            Tags = arguments.Tags ?? new Dictionary<string, string>(),
            Priority = ParsePriority(arguments.Priority),
            SecurityPolicy = arguments.SecurityPolicy
        };

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsError)
        {
            return CreateErrorResponse(
                $"Failed to send email: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        var emailResult = result.Value;
        
        return CreateJsonResponse(new
        {
            success = emailResult.Success,
            messageId = emailResult.MessageId,
            requestId = emailResult.RequestId,
            status = emailResult.Status.ToString(),
            duration = emailResult.Duration?.TotalMilliseconds ?? 0,
            metadata = emailResult.Metadata
        });
    }

    private static EmailPriority ParsePriority(string? priority)
    {
        return priority?.ToLowerInvariant() switch
        {
            "low" => EmailPriority.Low,
            "normal" => EmailPriority.Normal,
            "high" => EmailPriority.High,
            _ => EmailPriority.Normal
        };
    }
}