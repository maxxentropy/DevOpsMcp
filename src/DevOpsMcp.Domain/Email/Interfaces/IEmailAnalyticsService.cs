using ErrorOr;

namespace DevOpsMcp.Domain.Email.Interfaces;

/// <summary>
/// Service for email analytics and statistics
/// </summary>
public interface IEmailAnalyticsService
{
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
    /// Get statistics for a specific template
    /// </summary>
    Task<ErrorOr<EmailStatistics>> GetTemplateStatisticsAsync(
        string templateName,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Track email event
    /// </summary>
    Task<ErrorOr<bool>> TrackEmailEventAsync(
        string messageId,
        EmailEventType eventType,
        Dictionary<string, object>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get email event history
    /// </summary>
    Task<ErrorOr<List<EmailEvent>>> GetEmailEventsAsync(
        string messageId,
        CancellationToken cancellationToken = default);
}