using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Clients;

namespace DevOpsMcp.Infrastructure.Services;

public interface IAzureDevOpsClientFactory
{
    ProjectHttpClient CreateProjectClient();
    WorkItemTrackingHttpClient CreateWorkItemClient();
    BuildHttpClient CreateBuildClient();
    GitHttpClient CreateGitClient();
    ReleaseHttpClient2 CreateReleaseClient();
}