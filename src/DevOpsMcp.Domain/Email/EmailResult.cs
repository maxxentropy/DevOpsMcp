using System;
using System.Collections.Generic;

namespace DevOpsMcp.Domain.Email;

/// <summary>
/// Result of an email send operation
/// </summary>
public sealed class EmailResult
{
    /// <summary>
    /// Whether the email was sent successfully
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The original request ID
    /// </summary>
    public required string RequestId { get; init; }

    /// <summary>
    /// Message ID from the email service provider
    /// </summary>
    public string? MessageId { get; init; }

    /// <summary>
    /// Error message if sending failed
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Whether this is a transient error that can be retried
    /// </summary>
    public bool IsTransient { get; init; }

    /// <summary>
    /// When the email was sent (or attempted)
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Email status
    /// </summary>
    public EmailStatus Status { get; init; }

    /// <summary>
    /// Additional metadata from the email provider
    /// </summary>
    public Dictionary<string, string> Metadata { get; init; } = new();

    /// <summary>
    /// Number of retry attempts made
    /// </summary>
    public int RetryCount { get; init; }

    /// <summary>
    /// Total time taken to send (including retries)
    /// </summary>
    public TimeSpan? Duration { get; init; }
}

/// <summary>
/// Email delivery status
/// </summary>
public enum EmailStatus
{
    /// <summary>
    /// Email is queued for sending
    /// </summary>
    Queued,

    /// <summary>
    /// Email is being sent
    /// </summary>
    Sending,

    /// <summary>
    /// Email was sent successfully
    /// </summary>
    Sent,

    /// <summary>
    /// Email delivery failed
    /// </summary>
    Failed,

    /// <summary>
    /// Email bounced
    /// </summary>
    Bounced,

    /// <summary>
    /// Recipient marked as complaint/spam
    /// </summary>
    Complaint,

    /// <summary>
    /// Email was suppressed (blacklisted recipient)
    /// </summary>
    Suppressed
}