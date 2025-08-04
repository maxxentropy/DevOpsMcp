using DevOpsMcp.Domain.Email;
using ErrorOr;

namespace DevOpsMcp.Domain.Interfaces;

/// <summary>
/// Enhanced email service with bulk sending capability
/// </summary>
public interface IEnhancedEmailService : IEmailService
{
    /// <summary>
    /// Send bulk personalized emails using templates
    /// </summary>
    Task<ErrorOr<BulkEmailResult>> SendBulkEmailAsync(
        BulkEmailRequest request, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a single email using AWS SES template
    /// </summary>
    Task<ErrorOr<EmailResult>> SendTemplatedEmailAsync(
        string toAddress,
        string templateName,
        Dictionary<string, object> templateData,
        string? configurationSet = null,
        List<string>? cc = null,
        List<string>? bcc = null,
        string? replyTo = null,
        Dictionary<string, string>? tags = null,
        CancellationToken cancellationToken = default);
}