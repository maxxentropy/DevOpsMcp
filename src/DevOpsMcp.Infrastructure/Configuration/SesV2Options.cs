namespace DevOpsMcp.Infrastructure.Configuration;

/// <summary>
/// Configuration options for AWS Simple Email Service V2
/// </summary>
public sealed class SesV2Options
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "AWS:SES";

    /// <summary>
    /// From email address (must be verified in AWS SES)
    /// </summary>
    public string FromAddress { get; set; } = string.Empty;

    /// <summary>
    /// From display name
    /// </summary>
    public string? FromName { get; set; }

    /// <summary>
    /// Default configuration set name for tracking (optional)
    /// </summary>
    public string? DefaultConfigurationSet { get; set; }

    /// <summary>
    /// Reply-to addresses (optional)
    /// </summary>
    public List<string> ReplyToAddresses { get; } = new();

    /// <summary>
    /// Team member email addresses for team email functionality
    /// </summary>
    public Dictionary<string, string> TeamMembers { get; } = new();
}