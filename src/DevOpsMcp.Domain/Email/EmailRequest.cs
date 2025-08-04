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
    /// Email subject line (used when not using AWS template)
    /// </summary>
    public string? Subject { get; private init; }

    /// <summary>
    /// Name of the AWS SES template to use
    /// </summary>
    public string? TemplateName { get; private init; }

    /// <summary>
    /// Raw HTML content (used when not using template)
    /// </summary>
    public string? HtmlContent { get; private init; }

    /// <summary>
    /// Raw text content (used when not using template)
    /// </summary>
    public string? TextContent { get; private init; }

    /// <summary>
    /// Data to pass to the template for rendering
    /// </summary>
    public IReadOnlyDictionary<string, object> TemplateData { get; private init; }

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

    /// <summary>
    /// Configuration set to use for tracking
    /// </summary>
    public string? ConfigurationSet { get; }

    private EmailRequest(
        string to,
        List<string>? cc = null,
        List<string>? bcc = null,
        string? replyTo = null,
        EmailPriority priority = EmailPriority.Normal,
        string? correlationId = null,
        Dictionary<string, string>? tags = null,
        string? configurationSet = null)
    {
        if (string.IsNullOrWhiteSpace(to))
            throw new ArgumentException("Recipient address is required", nameof(to));

        Id = Guid.NewGuid().ToString();
        To = to;
        Cc = cc ?? new List<string>();
        Bcc = bcc ?? new List<string>();
        ReplyTo = replyTo;
        Priority = priority;
        CreatedAt = DateTime.UtcNow;
        CorrelationId = correlationId;
        Tags = tags ?? new Dictionary<string, string>();
        ConfigurationSet = configurationSet;
        TemplateData = new Dictionary<string, object>();
    }

    /// <summary>
    /// Create a template-based email request
    /// </summary>
    public static EmailRequest FromTemplate(
        string to,
        string templateName,
        Dictionary<string, object>? templateData = null,
        List<string>? cc = null,
        List<string>? bcc = null,
        string? replyTo = null,
        EmailPriority priority = EmailPriority.Normal,
        string? correlationId = null,
        Dictionary<string, string>? tags = null,
        string? configurationSet = null)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            throw new ArgumentException("Template name is required", nameof(templateName));

        var request = new EmailRequest(to, cc, bcc, replyTo, priority, correlationId, tags, configurationSet)
        {
            TemplateName = templateName,
            TemplateData = templateData ?? new Dictionary<string, object>()
        };
        return request;
    }

    /// <summary>
    /// Create a raw content email request
    /// </summary>
    public static EmailRequest FromContent(
        string to,
        string subject,
        string htmlContent,
        string? textContent = null,
        List<string>? cc = null,
        List<string>? bcc = null,
        string? replyTo = null,
        EmailPriority priority = EmailPriority.Normal,
        string? correlationId = null,
        Dictionary<string, string>? tags = null,
        string? configurationSet = null)
    {
        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentException("Subject is required", nameof(subject));
        if (string.IsNullOrWhiteSpace(htmlContent))
            throw new ArgumentException("HTML content is required", nameof(htmlContent));

        var request = new EmailRequest(to, cc, bcc, replyTo, priority, correlationId, tags, configurationSet)
        {
            Subject = subject,
            HtmlContent = htmlContent,
            TextContent = textContent,
            TemplateData = new Dictionary<string, object>()
        };
        return request;
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