using System.Text.Json;
using DevOpsMcp.Domain.Interfaces;
using DevOpsMcp.Server.Mcp;

namespace DevOpsMcp.Server.Tools.Email;

/// <summary>
/// MCP tool for getting AWS SES account information
/// </summary>
public sealed class GetSendStatisticsTool(IEmailAccountService emailAccountService) : BaseTool<GetSendStatisticsToolArguments>
{
    public override string Name => "get_send_statistics";
    
    public override string Description => 
        "Get AWS SES account status and configuration information.";
    
    public override JsonElement InputSchema => CreateSchema<GetSendStatisticsToolArguments>();

    protected override async Task<CallToolResponse> ExecuteInternalAsync(
        GetSendStatisticsToolArguments arguments, 
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await emailAccountService.GetAccountInfoAsync(cancellationToken);

            if (result.IsError)
            {
                return CreateErrorResponse($"Failed to get account information: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            var accountInfo = result.Value;
            return CreateJsonResponse(new
            {
                success = true,
                account = new
                {
                    sendingEnabled = accountInfo.SendingEnabled,
                    productionAccess = accountInfo.ProductionAccessEnabled,
                    enforcementStatus = accountInfo.EnforcementStatus ?? "Unknown",
                    suppressionAttributes = accountInfo.SuppressedReasons.Any() ? new
                    {
                        suppressedReasons = accountInfo.SuppressedReasons
                    } : null
                },
                note = "For detailed sending statistics, use AWS CloudWatch or the AWS Console. AWS SES V2 API provides metrics through CloudWatch."
            });
        }
        catch (Exception ex)
        {
            return CreateErrorResponse($"Unexpected error: {ex.Message}");
        }
    }
}