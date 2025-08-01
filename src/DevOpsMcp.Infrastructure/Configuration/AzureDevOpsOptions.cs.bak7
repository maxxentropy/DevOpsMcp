namespace DevOpsMcp.Infrastructure.Configuration;

public sealed class AzureDevOpsOptions
{
    public const string SectionName = "AzureDevOps";
    
    public string OrganizationUrl { get; set; } = string.Empty;
    public string PersonalAccessToken { get; set; } = string.Empty;
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? TenantId { get; set; }
    public AuthenticationMethod AuthMethod { get; set; } = AuthenticationMethod.PersonalAccessToken;
    public int TimeoutSeconds { get; set; } = 30;
    public int MaxRetryAttempts { get; set; } = 3;
    public bool EnableCaching { get; set; } = true;
    public int CacheExpirationMinutes { get; set; } = 5;
}

public enum AuthenticationMethod
{
    PersonalAccessToken,
    OAuth,
    AzureAD
}