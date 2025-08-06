using Amazon.SimpleEmailV2;
using Amazon.SimpleEmailV2.Model;
using DevOpsMcp.Domain.Email;
using DevOpsMcp.Domain.Interfaces;
using ErrorOr;
using Microsoft.Extensions.Logging;

namespace DevOpsMcp.Infrastructure.Email;

/// <summary>
/// AWS SES V2 account service implementation
/// </summary>
public sealed class SesV2AccountService : IEmailAccountService
{
    private readonly IAmazonSimpleEmailServiceV2 _sesClient;
    private readonly ILogger<SesV2AccountService> _logger;

    public SesV2AccountService(
        IAmazonSimpleEmailServiceV2 sesClient,
        ILogger<SesV2AccountService> logger)
    {
        _sesClient = sesClient ?? throw new ArgumentNullException(nameof(sesClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ErrorOr<EmailQuotaInfo>> GetSendQuotaAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _sesClient.GetAccountAsync(new GetAccountRequest(), cancellationToken);
            
            return new EmailQuotaInfo
            {
                SendingEnabled = response.SendingEnabled,
                ProductionAccessEnabled = response.ProductionAccessEnabled,
                EnforcementStatus = response.EnforcementStatus,
                ContactLanguage = response.Details?.ContactLanguage,
                SuppressedReasons = response.SuppressionAttributes?.SuppressedReasons ?? new List<string>(),
                VdmEnabled = response.VdmAttributes?.VdmEnabled == "ENABLED"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get send quota");
            return Error.Failure(ex.Message);
        }
    }

    public async Task<ErrorOr<EmailAccountInfo>> GetAccountInfoAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _sesClient.GetAccountAsync(new GetAccountRequest(), cancellationToken);
            
            return new EmailAccountInfo
            {
                SendingEnabled = response.SendingEnabled,
                ProductionAccessEnabled = response.ProductionAccessEnabled,
                EnforcementStatus = response.EnforcementStatus,
                SuppressedReasons = response.SuppressionAttributes?.SuppressedReasons ?? new List<string>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get account information");
            return Error.Failure(ex.Message);
        }
    }
}