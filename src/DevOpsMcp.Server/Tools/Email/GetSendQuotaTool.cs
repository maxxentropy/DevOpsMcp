using System.Text.Json;
using DevOpsMcp.Domain.Interfaces;
using DevOpsMcp.Server.Mcp;

namespace DevOpsMcp.Server.Tools.Email;

/// <summary>
/// MCP tool for getting AWS SES send quota
/// </summary>
public sealed class GetSendQuotaTool(IEmailAccountService emailAccountService) : BaseTool<GetSendQuotaToolArguments>
{
    public override string Name => "get_send_quota";
    
    public override string Description => 
        "Get the current AWS SES account sending quota and usage.";
    
    public override JsonElement InputSchema => CreateSchema<GetSendQuotaToolArguments>();

    protected override async Task<CallToolResponse> ExecuteInternalAsync(
        GetSendQuotaToolArguments arguments, 
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await emailAccountService.GetSendQuotaAsync(cancellationToken);

            if (result.IsError)
            {
                return CreateErrorResponse($"Failed to get send quota: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }

            var quota = result.Value;
            return CreateJsonResponse(new
            {
                success = true,
                sendingEnabled = quota.SendingEnabled,
                productionAccessEnabled = quota.ProductionAccessEnabled,
                enforcementStatus = quota.EnforcementStatus,
                details = quota.ContactLanguage != null ? new { value = quota.ContactLanguage } : null,
                suppressionAttributes = new
                {
                    suppressedReasons = quota.SuppressedReasons
                },
                vdmAttributes = quota.VdmEnabled.HasValue ? new
                {
                    enabled = quota.VdmEnabled.Value
                } : null
            });
        }
        catch (Exception ex)
        {
            return CreateErrorResponse($"Unexpected error: {ex.Message}");
        }
    }
}