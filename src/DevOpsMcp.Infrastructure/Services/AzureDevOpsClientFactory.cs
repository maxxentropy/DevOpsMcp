using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Clients;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Threading.Tasks;

namespace DevOpsMcp.Infrastructure.Services;

public class AzureDevOpsClientFactory : IAzureDevOpsClientFactory
{
    private readonly VssConnection _connection;
    private readonly string _organizationUrl;
    private bool _isInitialized;

    public AzureDevOpsClientFactory(string organizationUrl, string personalAccessToken)
    {
        if (string.IsNullOrWhiteSpace(organizationUrl))
            throw new ArgumentNullException(nameof(organizationUrl));
        
        if (string.IsNullOrWhiteSpace(personalAccessToken))
            throw new ArgumentNullException(nameof(personalAccessToken));
        
        _organizationUrl = organizationUrl;
        var credentials = new VssBasicCredential(string.Empty, personalAccessToken);
        _connection = new VssConnection(new Uri(organizationUrl), credentials);
    }
    
    private async Task EnsureConnectionAsync()
    {
        if (!_isInitialized)
        {
            await _connection.ConnectAsync();
            _isInitialized = true;
        }
    }

    public ProjectHttpClient CreateProjectClient()
    {
        return _connection.GetClient<ProjectHttpClient>();
    }

    public WorkItemTrackingHttpClient CreateWorkItemClient()
    {
        return _connection.GetClient<WorkItemTrackingHttpClient>();
    }

    public BuildHttpClient CreateBuildClient()
    {
        return _connection.GetClient<BuildHttpClient>();
    }

    public GitHttpClient CreateGitClient()
    {
        return _connection.GetClient<GitHttpClient>();
    }

    public ReleaseHttpClient2 CreateReleaseClient()
    {
        return _connection.GetClient<ReleaseHttpClient2>();
    }
}