namespace DevOpsMcp.Infrastructure.Configuration;

/// <summary>
/// General email configuration options
/// </summary>
public sealed class EmailOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "Email";

    /// <summary>
    /// Path to email templates
    /// </summary>
    public string TemplatesPath { get; set; } = "EmailTemplates";

    /// <summary>
    /// Cache duration for rendered templates in minutes
    /// </summary>
    public int CacheDurationMinutes { get; set; } = 60;

    /// <summary>
    /// Maximum retry attempts for failed sends
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Circuit breaker threshold
    /// </summary>
    public int CircuitBreakerThreshold { get; set; } = 5;

    /// <summary>
    /// Circuit breaker duration in seconds
    /// </summary>
    public int CircuitBreakerDurationSeconds { get; set; } = 60;

    /// <summary>
    /// Default security policy name
    /// </summary>
    public string DefaultSecurityPolicy { get; set; } = "Standard";

    /// <summary>
    /// Whether to use local SMTP for development
    /// </summary>
    public bool UseLocalSmtp { get; set; }

    /// <summary>
    /// Local SMTP host (for development)
    /// </summary>
    public string LocalSmtpHost { get; set; } = "localhost";

    /// <summary>
    /// Local SMTP port (for development)
    /// </summary>
    public int LocalSmtpPort { get; set; } = 1025;

    /// <summary>
    /// Whether to save sent emails to disk (for debugging)
    /// </summary>
    public bool SaveToDisk { get; set; }

    /// <summary>
    /// Path to save emails when SaveToDisk is true
    /// </summary>
    public string SavePath { get; set; } = "sent-emails";
}