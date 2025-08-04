using DevOpsMcp.Domain.Email;
using ErrorOr;

namespace DevOpsMcp.Domain.Interfaces;

/// <summary>
/// Enhanced email service with advanced capabilities like bulk sending, templates, and analytics
/// </summary>
public interface IEnhancedEmailService : IEmailService
{
    /// <summary>
    /// Send bulk personalized emails
    /// </summary>
    Task<ErrorOr<BulkEmailResult>> SendBulkEmailAsync(
        BulkEmailRequest request, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Create or update an email template
    /// </summary>
    Task<ErrorOr<SesEmailTemplate>> CreateTemplateAsync(
        SesEmailTemplate emailTemplate, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get an email template
    /// </summary>
    Task<ErrorOr<SesEmailTemplate>> GetTemplateAsync(
        string templateName, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete an email template
    /// </summary>
    Task<ErrorOr<bool>> DeleteTemplateAsync(
        string templateName, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List available email templates
    /// </summary>
    Task<ErrorOr<List<SesEmailTemplate>>> ListTemplatesAsync(
        int? pageSize = null,
        string? nextToken = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Add an email to the suppression list
    /// </summary>
    Task<ErrorOr<SuppressionEntry>> AddToSuppressionListAsync(
        string email, 
        SuppressionReason reason,
        string? description = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove an email from the suppression list
    /// </summary>
    Task<ErrorOr<bool>> RemoveFromSuppressionListAsync(
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current send quota
    /// </summary>
    Task<ErrorOr<SendQuota>> GetSendQuotaAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get email sending statistics
    /// </summary>
    Task<ErrorOr<EmailStatistics>> GetStatisticsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Test an email template with sample data
    /// </summary>
    Task<ErrorOr<RenderedTemplate>> TestTemplateAsync(
        string templateName,
        Dictionary<string, object> templateData,
        CancellationToken cancellationToken = default);
}