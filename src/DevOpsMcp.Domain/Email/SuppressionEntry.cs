namespace DevOpsMcp.Domain.Email;

/// <summary>
/// Represents an entry in the email suppression list
/// </summary>
public sealed class SuppressionEntry
{
    /// <summary>
    /// Email address that is suppressed
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Reason for suppression
    /// </summary>
    public SuppressionReason Reason { get; init; }

    /// <summary>
    /// Additional description or context
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// When this entry was added
    /// </summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>
    /// Source of the suppression (manual, bounce, complaint)
    /// </summary>
    public string Source { get; init; } = "Manual";

    /// <summary>
    /// Whether this suppression is active
    /// </summary>
    public bool IsActive { get; init; } = true;
}

/// <summary>
/// Reasons for email suppression
/// </summary>
public enum SuppressionReason
{
    /// <summary>
    /// User manually unsubscribed
    /// </summary>
    Unsubscribed,

    /// <summary>
    /// Email bounced (hard bounce)
    /// </summary>
    Bounced,

    /// <summary>
    /// Recipient complained (spam report)
    /// </summary>
    Complained,

    /// <summary>
    /// Manually added by administrator
    /// </summary>
    Manual,

    /// <summary>
    /// Invalid email format
    /// </summary>
    Invalid,

    /// <summary>
    /// Other reason
    /// </summary>
    Other
}