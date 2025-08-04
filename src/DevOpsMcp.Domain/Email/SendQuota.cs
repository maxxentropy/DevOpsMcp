namespace DevOpsMcp.Domain.Email;

/// <summary>
/// Represents the current email sending quota
/// </summary>
public sealed class SendQuota
{
    /// <summary>
    /// Maximum number of emails that can be sent per day
    /// </summary>
    public double Max24HourSend { get; init; }

    /// <summary>
    /// Maximum send rate (emails per second)
    /// </summary>
    public double MaxSendRate { get; init; }

    /// <summary>
    /// Number of emails sent in the last 24 hours
    /// </summary>
    public double SentLast24Hours { get; init; }

    /// <summary>
    /// Remaining quota for the current 24-hour period
    /// </summary>
    public double RemainingQuota => Max24HourSend - SentLast24Hours;

    /// <summary>
    /// Percentage of quota used
    /// </summary>
    public double UsagePercentage => Max24HourSend > 0 ? (SentLast24Hours / Max24HourSend) * 100 : 0;

    /// <summary>
    /// Whether we're approaching the quota limit (>80% used)
    /// </summary>
    public bool IsApproachingLimit => UsagePercentage > 80;

    /// <summary>
    /// Whether the quota has been exceeded
    /// </summary>
    public bool IsExceeded => SentLast24Hours >= Max24HourSend;
}