using DevOpsMcp.Infrastructure.Configuration;

namespace DevOpsMcp.Infrastructure.Authentication;

public interface IDevOpsAuthenticationProvider
{
    Task<AuthenticationResult> AuthenticateAsync(AuthenticationMethod method, CancellationToken cancellationToken = default);
    Task<bool> ValidatePermissionsAsync(string operation, string resource, CancellationToken cancellationToken = default);
}

public sealed record AuthenticationResult
{
    public required string AccessToken { get; init; }
    public required string TokenType { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public string? RefreshToken { get; init; }
    public List<string> Scopes { get; init; } = new();
}

public sealed class DevOpsAuthenticationProvider : IDevOpsAuthenticationProvider
{
    private readonly AzureDevOpsOptions _options;
    private readonly ILogger<DevOpsAuthenticationProvider> _logger;
    private AuthenticationResult? _cachedResult;

    public DevOpsAuthenticationProvider(
        IOptions<AzureDevOpsOptions> options,
        ILogger<DevOpsAuthenticationProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task<AuthenticationResult> AuthenticateAsync(AuthenticationMethod method, CancellationToken cancellationToken = default)
    {
        if (_cachedResult != null && _cachedResult.ExpiresAt > DateTime.UtcNow.AddMinutes(5))
        {
            return _cachedResult;
        }

        _cachedResult = method switch
        {
            AuthenticationMethod.PersonalAccessToken => await AuthenticateWithPATAsync(),
            AuthenticationMethod.OAuth => await AuthenticateWithOAuthAsync(cancellationToken),
            AuthenticationMethod.AzureAD => await AuthenticateWithAzureADAsync(cancellationToken),
            _ => throw new NotSupportedException($"Authentication method {method} is not supported")
        };

        return _cachedResult;
    }

    public async Task<bool> ValidatePermissionsAsync(string operation, string resource, CancellationToken cancellationToken = default)
    {
        try
        {
            // For PAT, we assume permissions are valid if authentication succeeds
            // For OAuth/AzureAD, we would check scopes/roles
            var auth = await AuthenticateAsync(_options.AuthMethod, cancellationToken);
            
            if (_options.AuthMethod == AuthenticationMethod.PersonalAccessToken)
            {
                return true;
            }

            // Check if required scope is present
            var requiredScope = GetRequiredScope(operation, resource);
            return auth.Scopes.Contains(requiredScope);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating permissions for {Operation} on {Resource}", operation, resource);
            return false;
        }
    }

    private Task<AuthenticationResult> AuthenticateWithPATAsync()
    {
        if (string.IsNullOrEmpty(_options.PersonalAccessToken))
        {
            throw new System.InvalidOperationException("Personal Access Token is not configured");
        }

        return Task.FromResult(new AuthenticationResult
        {
            AccessToken = _options.PersonalAccessToken,
            TokenType = "Basic",
            Scopes = new List<string> { "vso.work_full", "vso.code_full", "vso.build_execute", "vso.project_manage" }
        });
    }

    private Task<AuthenticationResult> AuthenticateWithOAuthAsync(CancellationToken cancellationToken)
    {
        // Implement OAuth flow
        throw new NotImplementedException("OAuth authentication is not yet implemented");
    }

    private async Task<AuthenticationResult> AuthenticateWithAzureADAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_options.TenantId) || 
            string.IsNullOrEmpty(_options.ClientId) || 
            string.IsNullOrEmpty(_options.ClientSecret))
        {
            throw new System.InvalidOperationException("Azure AD authentication requires TenantId, ClientId, and ClientSecret");
        }

        var credential = new Azure.Identity.ClientSecretCredential(
            _options.TenantId,
            _options.ClientId,
            _options.ClientSecret);

        var tokenRequestContext = new Azure.Core.TokenRequestContext(
            new[] { "499b84ac-1321-427f-aa17-267ca6975798/.default" });

        var accessToken = await credential.GetTokenAsync(tokenRequestContext, cancellationToken);

        return new AuthenticationResult
        {
            AccessToken = accessToken.Token,
            TokenType = "Bearer",
            ExpiresAt = accessToken.ExpiresOn.UtcDateTime,
            Scopes = new List<string> { "vso.work_full", "vso.code_full", "vso.build_execute", "vso.project_manage" }
        };
    }

    private static string GetRequiredScope(string operation, string resource)
    {
        return (operation.ToLowerInvariant(), resource.ToLowerInvariant()) switch
        {
            ("read", "workitem") => "vso.work",
            ("write", "workitem") => "vso.work_write",
            ("full", "workitem") => "vso.work_full",
            ("read", "code") => "vso.code",
            ("write", "code") => "vso.code_write",
            ("full", "code") => "vso.code_full",
            ("execute", "build") => "vso.build_execute",
            ("manage", "project") => "vso.project_manage",
            _ => "vso.work"
        };
    }
}