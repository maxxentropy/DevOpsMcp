namespace DevOpsMcp.Infrastructure.Configuration;

/// <summary>
/// Configuration options for AWS Simple Email Service
/// </summary>
public sealed class AwsSesOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "AWS:SES";

    /// <summary>
    /// AWS region for SES (e.g., "us-east-1")
    /// </summary>
    public string Region { get; set; } = "us-east-1";

    /// <summary>
    /// From email address
    /// </summary>
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>
    /// From display name
    /// </summary>
    public string FromName { get; set; } = "DevOps MCP";

    /// <summary>
    /// Configuration set name for tracking
    /// </summary>
    public string? ConfigurationSet { get; set; }

    /// <summary>
    /// Maximum send rate (emails per second)
    /// </summary>
    public int MaxSendRate { get; set; } = 14;

    /// <summary>
    /// Whether to use SES sandbox mode
    /// </summary>
    public bool UseSandbox { get; set; } = true;

    /// <summary>
    /// Reply-to address (if different from From)
    /// </summary>
    public string? ReplyToAddress { get; set; }

    /// <summary>
    /// Whether to enable detailed logging
    /// </summary>
    public bool EnableDetailedLogging { get; set; }

    /// <summary>
    /// Timeout for SES operations in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;
}