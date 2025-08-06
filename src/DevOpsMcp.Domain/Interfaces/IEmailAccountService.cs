using DevOpsMcp.Domain.Email;
using ErrorOr;

namespace DevOpsMcp.Domain.Interfaces;

/// <summary>
/// Service for AWS SES account operations and status monitoring
/// </summary>
public interface IEmailAccountService
{
    /// <summary>
    /// Get the current sending quota and usage information
    /// </summary>
    Task<ErrorOr<EmailQuotaInfo>> GetSendQuotaAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get account status and configuration information
    /// </summary>
    Task<ErrorOr<EmailAccountInfo>> GetAccountInfoAsync(CancellationToken cancellationToken = default);
}