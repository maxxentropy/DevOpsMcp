using System.Threading;
using System.Threading.Tasks;
using DevOpsMcp.Domain.Email;
using ErrorOr;

namespace DevOpsMcp.Domain.Interfaces;

/// <summary>
/// Service for sending emails
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send an email using the specified request
    /// </summary>
    Task<ErrorOr<EmailResult>> SendEmailAsync(EmailRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send an email with a specific security policy
    /// </summary>
    Task<ErrorOr<EmailResult>> SendEmailAsync(EmailRequest request, EmailSecurityPolicy policy, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the status of a previously sent email
    /// </summary>
    Task<ErrorOr<EmailStatus>> GetEmailStatusAsync(string messageId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate an email request without sending
    /// </summary>
    Task<ErrorOr<ValidationResult>> ValidateEmailAsync(EmailRequest request, CancellationToken cancellationToken = default);
}