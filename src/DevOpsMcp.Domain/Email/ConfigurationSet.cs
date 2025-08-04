namespace DevOpsMcp.Domain.Email;

/// <summary>
/// Represents an email configuration set for tracking and event publishing
/// </summary>
public sealed class ConfigurationSet
{
    /// <summary>
    /// Name of the configuration set
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Whether tracking is enabled
    /// </summary>
    public bool TrackingEnabled { get; set; } = true;

    /// <summary>
    /// Reputation tracking status
    /// </summary>
    public ReputationTrackingStatus ReputationTracking { get; set; } = ReputationTrackingStatus.Enabled;

    /// <summary>
    /// Sending status
    /// </summary>
    public SendingStatus SendingStatus { get; set; } = SendingStatus.Enabled;

    /// <summary>
    /// Event destinations for this configuration set
    /// </summary>
    public List<EventDestination> EventDestinations { get; } = new();

    /// <summary>
    /// Tags for categorization
    /// </summary>
    public Dictionary<string, string> Tags { get; } = new();

    /// <summary>
    /// When this configuration set was created
    /// </summary>
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Event destination for configuration set events
/// </summary>
public sealed class EventDestination
{
    /// <summary>
    /// Name of the event destination
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Whether this destination is enabled
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Event types to send to this destination
    /// </summary>
    public List<EmailEventType> EventTypes { get; } = new();

    /// <summary>
    /// Destination type
    /// </summary>
    public EventDestinationType DestinationType { get; set; }

    /// <summary>
    /// Configuration specific to the destination type
    /// </summary>
    public Dictionary<string, object> Configuration { get; } = new();
}

/// <summary>
/// Types of email events
/// </summary>
public enum EmailEventType
{
    Send,
    Bounce,
    Complaint,
    Delivery,
    Reject,
    Open,
    Click,
    RenderingFailure
}

/// <summary>
/// Types of event destinations
/// </summary>
public enum EventDestinationType
{
    CloudWatch,
    SNS,
    KinesisFirehose
}

/// <summary>
/// Reputation tracking status
/// </summary>
public enum ReputationTrackingStatus
{
    Enabled,
    Disabled
}

/// <summary>
/// Sending status
/// </summary>
public enum SendingStatus
{
    Enabled,
    Disabled
}