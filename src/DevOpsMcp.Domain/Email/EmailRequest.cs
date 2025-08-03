using System;
using System.Collections.Generic;

namespace DevOpsMcp.Domain.Email;

/// <summary>
/// Represents a request to send an email
/// </summary>
public sealed class EmailRequest
{
    /// <summary>
    /// Unique identifier for this email request
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Recipient email address
    /// </summary>
    public string To { get; }

    /// <summary>
    /// Optional CC recipients
    /// </summary>
    public IReadOnlyList<string> Cc { get; }

    /// <summary>
    /// Optional BCC recipients
    /// </summary>
    public IReadOnlyList<string> Bcc { get; }

    /// <summary>
    /// Email subject line
    /// </summary>
    public string Subject { get; }

    /// <summary>
    /// Name of the Razor template to use
    /// </summary>
    public string TemplateName { get; }

    /// <summary>
    /// Data to pass to the template for rendering
    /// </summary>
    public IReadOnlyDictionary<string, object> TemplateData { get; }

    /// <summary>
    /// Optional reply-to address
    /// </summary>
    public string? ReplyTo { get; }

    /// <summary>
    /// Email priority
    /// </summary>
    public EmailPriority Priority { get; }

    /// <summary>
    /// When this request was created
    /// </summary>
    public DateTime CreatedAt { get; }

    /// <summary>
    /// Optional correlation ID for tracking across services
    /// </summary>
    public string? CorrelationId { get; }

    /// <summary>
    /// Optional tags for categorization and filtering
    /// </summary>
    public IReadOnlyDictionary<string, string> Tags { get; }

    public EmailRequest(
        string to,
        string subject,
        string templateName,
        Dictionary<string, object>? templateData = null,
        List<string>? cc = null,
        List<string>? bcc = null,
        string? replyTo = null,
        EmailPriority priority = EmailPriority.Normal,
        string? correlationId = null,
        Dictionary<string, string>? tags = null)
    {
        if (string.IsNullOrWhiteSpace(to))
            throw new ArgumentException("Recipient address is required", nameof(to));
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject is required", nameof(subject));
        if (string.IsNullOrWhiteSpace(templateName))
            throw new ArgumentException("Template name is required", nameof(templateName));

        Id = Guid.NewGuid().ToString();
        To = to;
        Subject = subject;
        TemplateName = templateName;
        TemplateData = templateData ?? new Dictionary<string, object>();
        Cc = cc ?? new List<string>();
        Bcc = bcc ?? new List<string>();
        ReplyTo = replyTo;
        Priority = priority;
        CreatedAt = DateTime.UtcNow;
        CorrelationId = correlationId;
        Tags = tags ?? new Dictionary<string, string>();
    }
}

/// <summary>
/// Email priority levels
/// </summary>
public enum EmailPriority
{
    Low,
    Normal,
    High
}