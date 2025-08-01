namespace DevOpsMcp.Domain.ValueObjects;

public sealed record OrganizationUrl
{
    public string Value { get; }
    
    private OrganizationUrl(string value)
    {
        Value = value;
    }
    
    public static ErrorOr<OrganizationUrl> Create(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return Error.Validation("OrganizationUrl.Empty", "Organization URL cannot be empty");
        }
        
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return Error.Validation("OrganizationUrl.Invalid", "Organization URL must be a valid absolute URL");
        }
        
        if (!uri.Host.EndsWith("dev.azure.com", StringComparison.OrdinalIgnoreCase) &&
            !uri.Host.EndsWith("visualstudio.com", StringComparison.OrdinalIgnoreCase))
        {
            return Error.Validation("OrganizationUrl.InvalidHost", "Organization URL must be an Azure DevOps URL");
        }
        
        return new OrganizationUrl(url);
    }
    
    public string GetOrganizationName()
    {
        var uri = new Uri(Value);
        if (uri.Host.EndsWith("dev.azure.com", StringComparison.OrdinalIgnoreCase))
        {
            return uri.Segments[1].TrimEnd('/');
        }
        else
        {
            return uri.Host.Split('.')[0];
        }
    }
    
    public static implicit operator string(OrganizationUrl url) => url.Value;
}