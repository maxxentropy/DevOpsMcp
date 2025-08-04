namespace DevOpsMcp.Domain.Email;

/// <summary>
/// Result of a bulk email send operation
/// </summary>
public sealed class BulkEmailResult
{
    /// <summary>
    /// Request ID for tracking
    /// </summary>
    public string RequestId { get; init; } = string.Empty;

    /// <summary>
    /// Number of successfully sent emails
    /// </summary>
    public int SuccessCount { get; init; }

    /// <summary>
    /// Number of failed emails
    /// </summary>
    public int FailureCount { get; init; }

    /// <summary>
    /// Total number of emails attempted
    /// </summary>
    public int TotalCount => SuccessCount + FailureCount;

    /// <summary>
    /// Individual results for each destination
    /// </summary>
    public IReadOnlyList<BulkEmailEntryResult> Results { get; init; } = new List<BulkEmailEntryResult>();

    /// <summary>
    /// Time taken to process the bulk request
    /// </summary>
    public TimeSpan? Duration { get; init; }

    /// <summary>
    /// Whether all emails were sent successfully
    /// </summary>
    public bool IsFullSuccess => FailureCount == 0 && SuccessCount > 0;
}

/// <summary>
/// Result for an individual email in a bulk send
/// </summary>
public sealed class BulkEmailEntryResult
{
    /// <summary>
    /// Recipient email address
    /// </summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>
    /// Whether the email was sent successfully
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Message ID if successful
    /// </summary>
    public string? MessageId { get; init; }

    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Status code from the email service
    /// </summary>
    public string Status { get; init; } = "Unknown";
}