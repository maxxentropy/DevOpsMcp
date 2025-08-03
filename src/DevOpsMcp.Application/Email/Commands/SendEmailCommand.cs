using System;
using System.Collections.Generic;
using DevOpsMcp.Domain.Email;
using ErrorOr;
using MediatR;

namespace DevOpsMcp.Application.Email.Commands;

/// <summary>
/// Command to send an email
/// </summary>
public sealed record SendEmailCommand : IRequest<ErrorOr<EmailResult>>
{
    /// <summary>
    /// Email recipient
    /// </summary>
    public required string To { get; init; }

    /// <summary>
    /// CC recipients
    /// </summary>
    public IReadOnlyList<string> Cc { get; init; } = Array.Empty<string>();

    /// <summary>
    /// BCC recipients
    /// </summary>
    public IReadOnlyList<string> Bcc { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Email subject
    /// </summary>
    public required string Subject { get; init; }

    /// <summary>
    /// Template name to use
    /// </summary>
    public required string TemplateName { get; init; }

    /// <summary>
    /// Template data/model
    /// </summary>
    public required Dictionary<string, object> TemplateData { get; init; }

    /// <summary>
    /// Reply-to address (optional)
    /// </summary>
    public string? ReplyTo { get; init; }

    /// <summary>
    /// Email tags for tracking
    /// </summary>
    public Dictionary<string, string> Tags { get; init; } = new();

    /// <summary>
    /// Email priority
    /// </summary>
    public EmailPriority Priority { get; init; } = EmailPriority.Normal;

    /// <summary>
    /// Security policy override
    /// </summary>
    public string? SecurityPolicy { get; init; }
}