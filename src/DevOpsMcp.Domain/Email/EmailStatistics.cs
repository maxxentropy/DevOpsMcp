namespace DevOpsMcp.Domain.Email;

/// <summary>
/// Email sending statistics
/// </summary>
public sealed class EmailStatistics
{
    /// <summary>
    /// Start date for these statistics
    /// </summary>
    public DateTime StartDate { get; init; }

    /// <summary>
    /// End date for these statistics
    /// </summary>
    public DateTime EndDate { get; init; }

    /// <summary>
    /// Total emails sent
    /// </summary>
    public long SendCount { get; init; }

    /// <summary>
    /// Total bounced emails
    /// </summary>
    public long BounceCount { get; init; }

    /// <summary>
    /// Total complaints
    /// </summary>
    public long ComplaintCount { get; init; }

    /// <summary>
    /// Total delivered emails
    /// </summary>
    public long DeliveryCount { get; init; }

    /// <summary>
    /// Total rejected emails
    /// </summary>
    public long RejectCount { get; init; }

    /// <summary>
    /// Total opened emails (if tracking enabled)
    /// </summary>
    public long OpenCount { get; init; }

    /// <summary>
    /// Total clicked links (if tracking enabled)
    /// </summary>
    public long ClickCount { get; init; }

    /// <summary>
    /// Bounce rate percentage
    /// </summary>
    public double BounceRate => SendCount > 0 ? (double)BounceCount / SendCount * 100 : 0;

    /// <summary>
    /// Complaint rate percentage
    /// </summary>
    public double ComplaintRate => SendCount > 0 ? (double)ComplaintCount / SendCount * 100 : 0;

    /// <summary>
    /// Delivery rate percentage
    /// </summary>
    public double DeliveryRate => SendCount > 0 ? (double)DeliveryCount / SendCount * 100 : 0;

    /// <summary>
    /// Open rate percentage
    /// </summary>
    public double OpenRate => DeliveryCount > 0 ? (double)OpenCount / DeliveryCount * 100 : 0;

    /// <summary>
    /// Click rate percentage
    /// </summary>
    public double ClickRate => OpenCount > 0 ? (double)ClickCount / OpenCount * 100 : 0;

    /// <summary>
    /// Statistics broken down by day
    /// </summary>
    public List<DailyEmailStatistics> DailyStats { get; init; } = new();

    /// <summary>
    /// Top bounce reasons
    /// </summary>
    public Dictionary<string, int> BounceReasons { get; init; } = new();

    /// <summary>
    /// Statistics by email template
    /// </summary>
    public Dictionary<string, EmailStatistics> TemplateStats { get; init; } = new();
}

/// <summary>
/// Daily email statistics
/// </summary>
public sealed class DailyEmailStatistics
{
    /// <summary>
    /// Date for these statistics
    /// </summary>
    public DateTime Date { get; init; }

    /// <summary>
    /// Emails sent on this day
    /// </summary>
    public long SendCount { get; init; }

    /// <summary>
    /// Bounces on this day
    /// </summary>
    public long BounceCount { get; init; }

    /// <summary>
    /// Complaints on this day
    /// </summary>
    public long ComplaintCount { get; init; }

    /// <summary>
    /// Deliveries on this day
    /// </summary>
    public long DeliveryCount { get; init; }
}