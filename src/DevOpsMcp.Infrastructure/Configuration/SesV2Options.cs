namespace DevOpsMcp.Infrastructure.Configuration;

/// <summary>
/// Configuration options for AWS Simple Email Service V2
/// </summary>
public sealed class SesV2Options
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "AWS:SES:V2";

    /// <summary>
    /// AWS region for SES V2 (e.g., "us-east-1")
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
    /// Default configuration set name for tracking
    /// </summary>
    public string? DefaultConfigurationSet { get; set; }

    /// <summary>
    /// Maximum send rate (emails per second)
    /// </summary>
    public int MaxSendRate { get; set; } = 50;

    /// <summary>
    /// Maximum bulk send size per request
    /// </summary>
    public int MaxBulkSize { get; set; } = 50;

    /// <summary>
    /// Whether to use SES sandbox mode
    /// </summary>
    public bool UseSandbox { get; set; } = true;

    /// <summary>
    /// Reply-to addresses (if different from From)
    /// </summary>
    public List<string> ReplyToAddresses { get; set; } = new();

    /// <summary>
    /// Whether to enable detailed logging
    /// </summary>
    public bool EnableDetailedLogging { get; set; }

    /// <summary>
    /// Timeout for SES operations in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Whether to enable V2 features (feature flag)
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Template configuration
    /// </summary>
    public TemplateOptions Templates { get; set; } = new();

    /// <summary>
    /// Event configuration
    /// </summary>
    public EventOptions Events { get; set; } = new();

    /// <summary>
    /// Analytics configuration
    /// </summary>
    public AnalyticsOptions Analytics { get; set; } = new();

    /// <summary>
    /// Security configuration
    /// </summary>
    public SecurityOptions Security { get; set; } = new();

    /// <summary>
    /// Template management options
    /// </summary>
    public sealed class TemplateOptions
    {
        /// <summary>
        /// Cache duration for templates
        /// </summary>
        public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Maximum number of template versions to keep
        /// </summary>
        public int MaxVersions { get; set; } = 10;

        /// <summary>
        /// Whether to enable template validation
        /// </summary>
        public bool EnableValidation { get; set; } = true;
    }

    /// <summary>
    /// Event publishing options
    /// </summary>
    public sealed class EventOptions
    {
        /// <summary>
        /// Whether event publishing is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Event types to track
        /// </summary>
        public List<string> Types { get; set; } = new() 
        { 
            "Send", "Bounce", "Complaint", "Delivery", "Open", "Click" 
        };

        /// <summary>
        /// Batch size for event processing
        /// </summary>
        public int BatchSize { get; set; } = 100;

        /// <summary>
        /// Processing interval in seconds
        /// </summary>
        public int ProcessingInterval { get; set; } = 60;

        /// <summary>
        /// SNS topic ARN for events
        /// </summary>
        public string? SnsTopicArn { get; set; }

        /// <summary>
        /// CloudWatch log group for events
        /// </summary>
        public string? CloudWatchLogGroup { get; set; }
    }

    /// <summary>
    /// Analytics options
    /// </summary>
    public sealed class AnalyticsOptions
    {
        /// <summary>
        /// Data retention period in days
        /// </summary>
        public int RetentionDays { get; set; } = 90;

        /// <summary>
        /// Aggregation interval in seconds
        /// </summary>
        public int AggregationInterval { get; set; } = 300;

        /// <summary>
        /// Enable real-time analytics
        /// </summary>
        public bool EnableRealTime { get; set; } = true;

        /// <summary>
        /// Storage type for analytics data
        /// </summary>
        public string StorageType { get; set; } = "Memory"; // Memory, DynamoDB, S3
    }

    /// <summary>
    /// Security options
    /// </summary>
    public sealed class SecurityOptions
    {
        /// <summary>
        /// Enforce DKIM signing
        /// </summary>
        public bool EnforceDKIM { get; set; } = true;

        /// <summary>
        /// Enforce SPF validation
        /// </summary>
        public bool EnforceSPF { get; set; } = true;

        /// <summary>
        /// Enable suppression list
        /// </summary>
        public bool SuppressionListEnabled { get; set; } = true;

        /// <summary>
        /// Require secure transport (TLS)
        /// </summary>
        public bool RequireSecureTransport { get; set; } = true;

        /// <summary>
        /// Enable PII detection
        /// </summary>
        public bool EnablePiiDetection { get; set; } = false;

        /// <summary>
        /// Allowed recipient domains (empty = all allowed)
        /// </summary>
        public List<string> AllowedDomains { get; set; } = new();

        /// <summary>
        /// Blocked recipient domains
        /// </summary>
        public List<string> BlockedDomains { get; set; } = new();
    }
}