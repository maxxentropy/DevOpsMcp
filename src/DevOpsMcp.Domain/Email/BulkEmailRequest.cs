namespace DevOpsMcp.Domain.Email;

/// <summary>
/// Request to send bulk personalized emails
/// </summary>
public sealed class BulkEmailRequest
{
    /// <summary>
    /// Unique identifier for this bulk request
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Template name to use for all emails
    /// </summary>
    public string TemplateName { get; }

    /// <summary>
    /// List of destinations with personalization data
    /// </summary>
    public IReadOnlyList<BulkEmailDestination> Destinations { get; }

    /// <summary>
    /// Default template data for all recipients
    /// </summary>
    public IReadOnlyDictionary<string, object> DefaultTemplateData { get; }

    /// <summary>
    /// Configuration set to use for tracking
    /// </summary>
    public string? ConfigurationSet { get; }

    /// <summary>
    /// Reply-to addresses
    /// </summary>
    public IReadOnlyList<string> ReplyToAddresses { get; }

    /// <summary>
    /// Tags for categorization
    /// </summary>
    public IReadOnlyDictionary<string, string> Tags { get; }

    public BulkEmailRequest(
        string templateName,
        List<BulkEmailDestination> destinations,
        Dictionary<string, object>? defaultTemplateData = null,
        string? configurationSet = null,
        List<string>? replyToAddresses = null,
        Dictionary<string, string>? tags = null)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            throw new ArgumentException("Template name is required", nameof(templateName));
        if (destinations == null || destinations.Count == 0)
            throw new ArgumentException("At least one destination is required", nameof(destinations));

        Id = Guid.NewGuid().ToString();
        TemplateName = templateName;
        Destinations = destinations;
        DefaultTemplateData = defaultTemplateData ?? new Dictionary<string, object>();
        ConfigurationSet = configurationSet;
        ReplyToAddresses = replyToAddresses ?? new List<string>();
        Tags = tags ?? new Dictionary<string, string>();
    }
}

/// <summary>
/// Individual destination for bulk email
/// </summary>
public sealed class BulkEmailDestination
{
    /// <summary>
    /// Recipient email address
    /// </summary>
    public string Email { get; }

    /// <summary>
    /// Personalized template data for this recipient
    /// </summary>
    public IReadOnlyDictionary<string, object> TemplateData { get; }

    public BulkEmailDestination(string email, Dictionary<string, object>? templateData = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email address is required", nameof(email));

        Email = email;
        TemplateData = templateData ?? new Dictionary<string, object>();
    }
}