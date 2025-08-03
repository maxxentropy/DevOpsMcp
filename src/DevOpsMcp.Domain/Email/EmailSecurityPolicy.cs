using System;
using System.Collections.Generic;
using System.Linq;

namespace DevOpsMcp.Domain.Email;

/// <summary>
/// Security policy for email sending
/// </summary>
public sealed class EmailSecurityPolicy
{
    /// <summary>
    /// Policy name
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Maximum recipients per email
    /// </summary>
    public int MaxRecipients { get; }

    /// <summary>
    /// Maximum emails per hour
    /// </summary>
    public int MaxEmailsPerHour { get; }

    /// <summary>
    /// Maximum emails per day
    /// </summary>
    public int MaxEmailsPerDay { get; }

    /// <summary>
    /// Allowed recipient domains (empty = all allowed)
    /// </summary>
    public IReadOnlyList<string> AllowedDomains { get; }

    /// <summary>
    /// Blocked recipient domains
    /// </summary>
    public IReadOnlyList<string> BlockedDomains { get; }

    /// <summary>
    /// Whether to require TLS for sending
    /// </summary>
    public bool RequireTls { get; }

    /// <summary>
    /// Whether to scan for sensitive data
    /// </summary>
    public bool ScanForSensitiveData { get; }

    /// <summary>
    /// Maximum attachment size in MB
    /// </summary>
    public int MaxAttachmentSizeMb { get; }

    /// <summary>
    /// Allowed attachment types (empty = all allowed)
    /// </summary>
    public IReadOnlyList<string> AllowedAttachmentTypes { get; }

    /// <summary>
    /// Whether to log email content
    /// </summary>
    public bool LogEmailContent { get; }

    /// <summary>
    /// Retention period for email logs in days
    /// </summary>
    public int LogRetentionDays { get; }

    private EmailSecurityPolicy(
        string name,
        int maxRecipients,
        int maxEmailsPerHour,
        int maxEmailsPerDay,
        List<string>? allowedDomains,
        List<string>? blockedDomains,
        bool requireTls,
        bool scanForSensitiveData,
        int maxAttachmentSizeMb,
        List<string>? allowedAttachmentTypes,
        bool logEmailContent,
        int logRetentionDays)
    {
        Name = name;
        MaxRecipients = maxRecipients;
        MaxEmailsPerHour = maxEmailsPerHour;
        MaxEmailsPerDay = maxEmailsPerDay;
        AllowedDomains = allowedDomains ?? new List<string>();
        BlockedDomains = blockedDomains ?? new List<string>();
        RequireTls = requireTls;
        ScanForSensitiveData = scanForSensitiveData;
        MaxAttachmentSizeMb = maxAttachmentSizeMb;
        AllowedAttachmentTypes = allowedAttachmentTypes ?? new List<string>();
        LogEmailContent = logEmailContent;
        LogRetentionDays = logRetentionDays;
    }

    /// <summary>
    /// Development policy - permissive for testing
    /// </summary>
    public static EmailSecurityPolicy Development => new(
        name: "Development",
        maxRecipients: 10,
        maxEmailsPerHour: 100,
        maxEmailsPerDay: 1000,
        allowedDomains: null,
        blockedDomains: null,
        requireTls: false,
        scanForSensitiveData: false,
        maxAttachmentSizeMb: 25,
        allowedAttachmentTypes: null,
        logEmailContent: true,
        logRetentionDays: 7
    );

    /// <summary>
    /// Standard policy - balanced security
    /// </summary>
    public static EmailSecurityPolicy Standard => new(
        name: "Standard",
        maxRecipients: 50,
        maxEmailsPerHour: 500,
        maxEmailsPerDay: 5000,
        allowedDomains: null,
        blockedDomains: new List<string> { "tempmail.com", "guerrillamail.com" },
        requireTls: true,
        scanForSensitiveData: true,
        maxAttachmentSizeMb: 10,
        allowedAttachmentTypes: new List<string> { ".pdf", ".docx", ".xlsx", ".png", ".jpg" },
        logEmailContent: false,
        logRetentionDays: 30
    );

    /// <summary>
    /// Restricted policy - high security
    /// </summary>
    public static EmailSecurityPolicy Restricted => new(
        name: "Restricted",
        maxRecipients: 20,
        maxEmailsPerHour: 100,
        maxEmailsPerDay: 500,
        allowedDomains: null,
        blockedDomains: new List<string> { "tempmail.com", "guerrillamail.com", "mailinator.com" },
        requireTls: true,
        scanForSensitiveData: true,
        maxAttachmentSizeMb: 5,
        allowedAttachmentTypes: new List<string> { ".pdf", ".docx" },
        logEmailContent: false,
        logRetentionDays: 90
    );

    /// <summary>
    /// Validate an email request against this policy
    /// </summary>
    public ValidationResult ValidateRequest(EmailRequest request)
    {
        var errors = new List<string>();

        // Check recipient count
        var totalRecipients = 1 + request.Cc.Count + request.Bcc.Count;
        if (totalRecipients > MaxRecipients)
        {
            errors.Add($"Too many recipients: {totalRecipients} (max: {MaxRecipients})");
        }

        // Check allowed domains
        var allRecipients = new[] { request.To }
            .Concat(request.Cc)
            .Concat(request.Bcc);

        foreach (var recipient in allRecipients)
        {
            var domain = recipient.Split('@').LastOrDefault()?.ToLowerInvariant();
            if (string.IsNullOrEmpty(domain))
            {
                errors.Add($"Invalid email address: {recipient}");
                continue;
            }

            if (BlockedDomains.Any(d => d.Equals(domain, StringComparison.OrdinalIgnoreCase)))
            {
                errors.Add($"Blocked domain: {domain}");
            }

            if (AllowedDomains.Any() && !AllowedDomains.Any(d => d.Equals(domain, StringComparison.OrdinalIgnoreCase)))
            {
                errors.Add($"Domain not allowed: {domain}");
            }
        }

        return new ValidationResult
        {
            IsValid = !errors.Any(),
            Errors = errors
        };
    }
}

/// <summary>
/// Result of security validation
/// </summary>
public sealed class ValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = new List<string>();
}