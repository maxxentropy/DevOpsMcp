namespace DevOpsMcp.Domain.Email;

/// <summary>
/// Represents an email event (send, bounce, open, click, etc.)
/// </summary>
public sealed class EmailEvent
{
    /// <summary>
    /// Unique event identifier
    /// </summary>
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// Message ID this event relates to
    /// </summary>
    public string MessageId { get; init; } = string.Empty;

    /// <summary>
    /// Type of event
    /// </summary>
    public EmailEventType Type { get; init; }

    /// <summary>
    /// When the event occurred
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Email address associated with the event
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Event-specific data
    /// </summary>
    public Dictionary<string, object> EventData { get; init; } = new();

    /// <summary>
    /// For bounce events, the bounce type
    /// </summary>
    public string? BounceType { get; init; }

    /// <summary>
    /// For bounce events, the bounce subtype
    /// </summary>
    public string? BounceSubType { get; init; }

    /// <summary>
    /// For click events, the URL clicked
    /// </summary>
    public string? ClickedUrl { get; init; }

    /// <summary>
    /// User agent string (for open/click events)
    /// </summary>
    public string? UserAgent { get; init; }

    /// <summary>
    /// IP address (for open/click events)
    /// </summary>
    public string? IpAddress { get; init; }
}