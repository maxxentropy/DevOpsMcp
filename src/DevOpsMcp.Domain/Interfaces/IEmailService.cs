using System.Threading;
using System.Threading.Tasks;
using DevOpsMcp.Domain.Email;
using ErrorOr;

namespace DevOpsMcp.Domain.Interfaces;

/// <summary>
/// Service for sending emails through AWS SES
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send an email (HTML or text)
    /// </summary>
    Task<ErrorOr<EmailResult>> SendEmailAsync(
        string toAddress, 
        string subject, 
        string body, 
        bool isHtml = true,
        List<string>? cc = null,
        List<string>? bcc = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send a templated email using AWS SES template
    /// </summary>
    Task<ErrorOr<EmailResult>> SendTemplatedEmailAsync(
        string toAddress,
        string templateName,
        Dictionary<string, object> templateData,
        List<string>? cc = null,
        List<string>? bcc = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send email to multiple team members
    /// </summary>
    Task<ErrorOr<List<EmailResult>>> SendTeamEmailAsync(
        List<string> teamEmails,
        string subject,
        string body,
        bool isHtml = true,
        CancellationToken cancellationToken = default);
}