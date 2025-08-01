using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Clients;
using Microsoft.VisualStudio.Services.WebApi;

namespace DevOpsMcp.Infrastructure.Services;

public class AzureDevOpsClientFactory : IAzureDevOpsClientFactory
{
    private readonly VssConnection _connection;

    public AzureDevOpsClientFactory(string organizationUrl, string personalAccessToken)
    {
        var credentials = new VssBasicCredential(string.Empty, personalAccessToken);
        _connection = new VssConnection(new Uri(organizationUrl), credentials);
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