using System.Text.Json;
using DevOpsMcp.Domain.Interfaces;
using DevOpsMcp.Infrastructure.Configuration;
using DevOpsMcp.Server.Mcp;
using Microsoft.Extensions.Options;

namespace DevOpsMcp.Server.Tools.Email;

/// <summary>
/// MCP tool for sending emails to team members
/// </summary>
public sealed class SendTeamEmailTool(
    IEmailService emailService,
    IOptions<SesV2Options> options) : BaseTool<SendTeamEmailToolArguments>
{
    private readonly SesV2Options _options = options.Value;

    public override string Name => "send_team_email";
    
    public override string Description => 
        "Send an email to all configured team members.";
    
    public override JsonElement InputSchema => CreateSchema<SendTeamEmailToolArguments>();

    protected override async Task<CallToolResponse> ExecuteInternalAsync(
        SendTeamEmailToolArguments arguments, 
        CancellationToken cancellationToken)
    {
        try
        {
            var teamEmails = _options.TeamMembers.Values.ToList();
            
            if (teamEmails.Count == 0)
            {
                return CreateErrorResponse("No team members configured. Add team members in configuration.");
            }

            var result = await emailService.SendTeamEmailAsync(
                teamEmails: teamEmails,
                subject: arguments.Subject,
                body: arguments.Body,
                isHtml: arguments.IsHtml ?? true,
                cancellationToken: cancellationToken);

            if (result.IsError)
            {
                return CreateErrorResponse($"Failed to send team email: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            var successCount = result.Value.Count;
            var totalCount = teamEmails.Count;

            return CreateJsonResponse(new
            {
                success = true,
                successCount,
                totalCount,
                failedCount = totalCount - successCount,
                results = result.Value.Select(r => new
                {
                    messageId = r.MessageId,
                    success = r.Success
                })
            });
        }
        catch (Exception ex)
        {
            return CreateErrorResponse($"Unexpected error: {ex.Message}");
        }
    }
}