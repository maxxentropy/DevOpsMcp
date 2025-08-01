using Microsoft.TeamFoundation.Build.WebApi;
using DevOpsMcp.Infrastructure.Services;
using DomainBuild = DevOpsMcp.Domain.Entities.Build;
using DomainBuildStatus = DevOpsMcp.Domain.Entities.BuildStatus;
using DomainBuildResult = DevOpsMcp.Domain.Entities.BuildResult;
using DomainBuildReason = DevOpsMcp.Domain.Entities.BuildReason;
using DomainBuildDefinitionReference = DevOpsMcp.Domain.Entities.BuildDefinitionReference;
using DomainBuildDefinitionType = DevOpsMcp.Domain.Entities.BuildDefinitionType;
using DomainBuildArtifact = DevOpsMcp.Domain.Interfaces.BuildArtifact;
using ApiBuild = Microsoft.TeamFoundation.Build.WebApi.Build;
using ApiBuildStatus = Microsoft.TeamFoundation.Build.WebApi.BuildStatus;
using ApiBuildResult = Microsoft.TeamFoundation.Build.WebApi.BuildResult;
using ApiBuildReason = Microsoft.TeamFoundation.Build.WebApi.BuildReason;
using ApiBuildArtifact = Microsoft.TeamFoundation.Build.WebApi.BuildArtifact;

namespace DevOpsMcp.Infrastructure.Repositories;

public sealed class BuildRepository(
    IAzureDevOpsClientFactory clientFactory,
    ILogger<BuildRepository> logger)
    : IBuildRepository
{
    public async Task<DomainBuild?> GetByIdAsync(string projectId, int buildId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = clientFactory.CreateBuildClient();
            var build = await client.GetBuildAsync(projectId, buildId, cancellationToken: cancellationToken);
            
            return MapToEntity(build);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting build {BuildId} in project {ProjectId}", buildId, projectId);
            return null;
        }
    }

    public async Task<IReadOnlyList<DomainBuild>> GetBuildsAsync(string projectId, BuildFilter? filter = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = clientFactory.CreateBuildClient();
            
            var builds = await client.GetBuildsAsync(
                projectId,
                definitions: filter?.DefinitionId != null ? new[] { filter.DefinitionId.Value } : null,
                statusFilter: MapBuildStatus(filter?.Status),
                resultFilter: MapBuildResult(filter?.Result),
                branchName: filter?.BranchName,
                minFinishTime: filter?.MinTime,
                maxFinishTime: filter?.MaxTime,
                top: filter?.Top,
                cancellationToken: cancellationToken);
            
            return builds.Select(b => MapToEntity(b)).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting builds in project {ProjectId}", projectId);
            throw;
        }
    }

    public async Task<DomainBuild> QueueBuildAsync(string projectId, QueueBuildRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = clientFactory.CreateBuildClient();
            
            var definition = await client.GetDefinitionAsync(projectId, request.DefinitionId, cancellationToken: cancellationToken);
            
            var build = new Microsoft.TeamFoundation.Build.WebApi.Build
            {
                Definition = definition,
                SourceBranch = request.SourceBranch ?? definition.Repository.DefaultBranch,
                Parameters = request.Parameters != null ? System.Text.Json.JsonSerializer.Serialize(request.Parameters) : null,
                Reason = ParseBuildReason(request.Reason)
            };

            var queuedBuild = await client.QueueBuildAsync(build, cancellationToken: cancellationToken);
            
            return MapToEntity(queuedBuild);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error queuing build in project {ProjectId}", projectId);
            throw;
        }
    }

    public async Task<DomainBuild> UpdateBuildAsync(string projectId, int buildId, BuildUpdateRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = clientFactory.CreateBuildClient();
            
            var build = await client.GetBuildAsync(projectId, buildId, cancellationToken: cancellationToken);
            
            if (request.KeepForever.HasValue)
            {
                // KeepForever is obsolete, use retention instead
                // build.KeepForever = request.KeepForever.Value;
                // TODO: Implement retention lease logic
            }
            
            if (request.RetainIndefinitely.HasValue)
            {
                build.RetainedByRelease = request.RetainIndefinitely.Value;
            }

            var updatedBuild = await client.UpdateBuildAsync(build, cancellationToken: cancellationToken);
            
            return MapToEntity(updatedBuild);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating build {BuildId} in project {ProjectId}", buildId, projectId);
            throw;
        }
    }

    public async Task<DomainBuild> CancelBuildAsync(string projectId, int buildId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = clientFactory.CreateBuildClient();
            
            var build = await client.GetBuildAsync(projectId, buildId, cancellationToken: cancellationToken);
            build.Status = Microsoft.TeamFoundation.Build.WebApi.BuildStatus.Cancelling;
            
            var cancelledBuild = await client.UpdateBuildAsync(build, cancellationToken: cancellationToken);
            
            return MapToEntity(cancelledBuild);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error cancelling build {BuildId} in project {ProjectId}", buildId, projectId);
            throw;
        }
    }

    public async Task<string> GetBuildLogsAsync(string projectId, int buildId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = clientFactory.CreateBuildClient();
            
            var logs = await client.GetBuildLogsAsync(projectId, buildId, cancellationToken: cancellationToken);
            
            if (!logs.Any())
            {
                return string.Empty;
            }

            var logContent = new System.Text.StringBuilder();
            
            foreach (var log in logs)
            {
                var lines = await client.GetBuildLogLinesAsync(projectId, buildId, log.Id, cancellationToken: cancellationToken);
                logContent.AppendLine($"=== Log: {log.Type} ===");
                foreach (var line in lines)
                {
                    logContent.AppendLine(line);
                }
                logContent.AppendLine();
            }
            
            return logContent.ToString();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting build logs for build {BuildId} in project {ProjectId}", buildId, projectId);
            throw;
        }
    }

    public async Task<IReadOnlyList<DomainBuildArtifact>> GetBuildArtifactsAsync(string projectId, int buildId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = clientFactory.CreateBuildClient();
            
            var artifacts = await client.GetArtifactsAsync(projectId, buildId, cancellationToken: cancellationToken);
            
            return artifacts.Select(a => new DomainBuildArtifact
            {
                Id = a.Id,
                Name = a.Name,
                Resource = a.Resource?.DownloadUrl ?? string.Empty
            }).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error getting build artifacts for build {BuildId} in project {ProjectId}", buildId, projectId);
            throw;
        }
    }

    private static DomainBuild MapToEntity(ApiBuild azureBuild)
    {
        return new DomainBuild
        {
            Id = azureBuild.Id,
            BuildNumber = azureBuild.BuildNumber,
            Status = MapBuildStatus(azureBuild.Status),
            Result = MapBuildResult(azureBuild.Result),
            QueueTime = azureBuild.QueueTime ?? DateTime.UtcNow,
            StartTime = azureBuild.StartTime,
            FinishTime = azureBuild.FinishTime,
            SourceBranch = azureBuild.SourceBranch ?? string.Empty,
            SourceVersion = azureBuild.SourceVersion ?? string.Empty,
            Definition = new DomainBuildDefinitionReference
            {
                Id = azureBuild.Definition.Id,
                Name = azureBuild.Definition.Name,
                Path = azureBuild.Definition.Path ?? "\\",
                Type = BuildDefinitionType.Yaml
            },
            RequestedFor = azureBuild.RequestedFor?.DisplayName ?? "Unknown",
            RequestedBy = azureBuild.RequestedBy?.DisplayName ?? "Unknown",
            Reason = MapBuildReason(azureBuild.Reason),
            Parameters = ParseParameters(azureBuild.Parameters),
            Tags = azureBuild.Tags?.ToList() ?? new List<string>()
        };
    }

    private static DomainBuildStatus MapBuildStatus(ApiBuildStatus? status)
    {
        return status switch
        {
            ApiBuildStatus.InProgress => DomainBuildStatus.InProgress,
            ApiBuildStatus.Completed => DomainBuildStatus.Completed,
            ApiBuildStatus.Cancelling => DomainBuildStatus.Cancelling,
            ApiBuildStatus.Postponed => DomainBuildStatus.Postponed,
            ApiBuildStatus.NotStarted => DomainBuildStatus.NotStarted,
            // ApiBuildStatus.Paused => DomainBuildStatus.Paused, // Not available in API
            _ => DomainBuildStatus.None
        };
    }

    private static ApiBuildStatus? MapBuildStatus(DomainBuildStatus? status)
    {
        return status switch
        {
            DomainBuildStatus.InProgress => ApiBuildStatus.InProgress,
            DomainBuildStatus.Completed => ApiBuildStatus.Completed,
            DomainBuildStatus.Cancelling => ApiBuildStatus.Cancelling,
            DomainBuildStatus.Postponed => ApiBuildStatus.Postponed,
            DomainBuildStatus.NotStarted => ApiBuildStatus.NotStarted,
            DomainBuildStatus.Paused => ApiBuildStatus.None, // Map to None as Paused is not available
            _ => null
        };
    }

    private static DomainBuildResult MapBuildResult(ApiBuildResult? result)
    {
        return result switch
        {
            ApiBuildResult.Succeeded => DomainBuildResult.Succeeded,
            ApiBuildResult.PartiallySucceeded => DomainBuildResult.PartiallySucceeded,
            ApiBuildResult.Failed => DomainBuildResult.Failed,
            ApiBuildResult.Canceled => DomainBuildResult.Canceled,
            _ => DomainBuildResult.None
        };
    }

    private static ApiBuildResult? MapBuildResult(DomainBuildResult? result)
    {
        return result switch
        {
            DomainBuildResult.Succeeded => ApiBuildResult.Succeeded,
            DomainBuildResult.PartiallySucceeded => ApiBuildResult.PartiallySucceeded,
            DomainBuildResult.Failed => ApiBuildResult.Failed,
            DomainBuildResult.Canceled => ApiBuildResult.Canceled,
            _ => null
        };
    }

    private static DomainBuildReason MapBuildReason(ApiBuildReason reason)
    {
        return reason switch
        {
            ApiBuildReason.Manual => DomainBuildReason.Manual,
            ApiBuildReason.IndividualCI => DomainBuildReason.IndividualCI,
            ApiBuildReason.BatchedCI => DomainBuildReason.BatchedCI,
            ApiBuildReason.Schedule => DomainBuildReason.Schedule,
            ApiBuildReason.UserCreated => DomainBuildReason.UserCreated,
            ApiBuildReason.PullRequest => DomainBuildReason.PullRequest,
            ApiBuildReason.BuildCompletion => DomainBuildReason.BuildCompletion,
            ApiBuildReason.ResourceTrigger => DomainBuildReason.ResourceTrigger,
            _ => DomainBuildReason.None
        };
    }

    private static ApiBuildReason ParseBuildReason(string? reason)
    {
        return reason?.ToLowerInvariant() switch
        {
            "manual" => ApiBuildReason.Manual,
            "individualci" => ApiBuildReason.IndividualCI,
            "batchedci" => ApiBuildReason.BatchedCI,
            "schedule" => ApiBuildReason.Schedule,
            "pullrequest" => ApiBuildReason.PullRequest,
            _ => ApiBuildReason.Manual
        };
    }

    private static Dictionary<string, string> ParseParameters(string? parameters)
    {
        if (string.IsNullOrEmpty(parameters))
        {
            return new Dictionary<string, string>();
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(parameters) 
                ?? new Dictionary<string, string>();
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }
}