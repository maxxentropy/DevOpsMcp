using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.Operations;
using DevOpsMcp.Infrastructure.Services;
using DomainProject = DevOpsMcp.Domain.Entities.Project;
using DomainProjectVisibility = DevOpsMcp.Domain.Entities.ProjectVisibility;
using DomainProjectState = DevOpsMcp.Domain.Entities.ProjectState;
using ApiProject = Microsoft.TeamFoundation.Core.WebApi.TeamProject;
using ApiProjectVisibility = Microsoft.TeamFoundation.Core.WebApi.ProjectVisibility;
using ApiProjectState = Microsoft.TeamFoundation.Core.WebApi.ProjectState;

namespace DevOpsMcp.Infrastructure.Repositories;

public sealed class ProjectRepository : IProjectRepository
{
    private readonly IAzureDevOpsClientFactory _clientFactory;
    private readonly ILogger<ProjectRepository> _logger;

    public ProjectRepository(
        IAzureDevOpsClientFactory clientFactory,
        ILogger<ProjectRepository> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    public async Task<DomainProject?> GetByIdAsync(string projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _clientFactory.CreateProjectClient();
            var project = await client.GetProject(projectId);
            
            return MapToEntity(project);
        }
        catch (ProjectDoesNotExistException)
        {
            _logger.LogWarning("Project {ProjectId} not found", projectId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting project {ProjectId}", projectId);
            throw;
        }
    }

    public async Task<IReadOnlyList<DomainProject>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _clientFactory.CreateProjectClient();
            var projects = await client.GetProjects();
            
            var fullProjects = new List<DomainProject>();
            foreach (var projectRef in projects)
            {
                var fullProject = await client.GetProject(projectRef.Id.ToString());
                fullProjects.Add(MapToEntity(fullProject));
            }
            return fullProjects;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all projects");
            throw;
        }
    }

    public async Task<DomainProject> CreateAsync(DomainProject project, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _clientFactory.CreateProjectClient();
            
            var projectToCreate = new TeamProject
            {
                Name = project.Name,
                Description = project.Description,
                Visibility = MapVisibility(project.Visibility),
                Capabilities = new Dictionary<string, Dictionary<string, string>>
                {
                    ["versioncontrol"] = new() { ["sourceControlType"] = "Git" },
                    ["processTemplate"] = new() { ["templateTypeId"] = "6b724908-ef14-45cf-84f8-768b5384da45" }
                }
            };

            var operation = await client.QueueCreateProject(projectToCreate);
            
            // Wait for operation to complete
            var createdProject = await WaitForProjectCreation(client, operation.Id, cancellationToken);
            
            return MapToEntity(createdProject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating project {ProjectName}", project.Name);
            throw;
        }
    }

    public async Task<DomainProject> UpdateAsync(DomainProject project, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _clientFactory.CreateProjectClient();
            
            var projectToUpdate = new TeamProject
            {
                Id = Guid.Parse(project.Id),
                Name = project.Name,
                Description = project.Description,
                Visibility = MapVisibility(project.Visibility)
            };

            var operation = await client.UpdateProject(Guid.Parse(project.Id), projectToUpdate);
            
            // Get updated project
            var updatedProject = await client.GetProject(project.Id);
            
            return MapToEntity(updatedProject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating project {ProjectId}", project.Id);
            throw;
        }
    }

    public async Task DeleteAsync(string projectId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _clientFactory.CreateProjectClient();
            await client.QueueDeleteProject(Guid.Parse(projectId));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting project {ProjectId}", projectId);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(string projectId, CancellationToken cancellationToken = default)
    {
        var project = await GetByIdAsync(projectId, cancellationToken);
        return project != null;
    }

    private static DomainProject MapToEntity(TeamProject teamProject)
    {
        return new DomainProject
        {
            Id = teamProject.Id.ToString(),
            Name = teamProject.Name,
            Description = teamProject.Description ?? string.Empty,
            OrganizationUrl = teamProject.Url,
            Visibility = MapVisibility(teamProject.Visibility),
            State = MapState(teamProject.State),
            CreatedDate = DateTime.UtcNow,
            LastUpdateTime = teamProject.LastUpdateTime
        };
    }

    private static DomainProjectVisibility MapVisibility(ApiProjectVisibility? visibility)
    {
        return visibility switch
        {
            ApiProjectVisibility.Public => DomainProjectVisibility.Public,
            ApiProjectVisibility.Organization => DomainProjectVisibility.Organization,
            _ => DomainProjectVisibility.Private
        };
    }

    private static ApiProjectVisibility MapVisibility(DomainProjectVisibility visibility)
    {
        return visibility switch
        {
            DomainProjectVisibility.Public => ApiProjectVisibility.Public,
            DomainProjectVisibility.Organization => ApiProjectVisibility.Organization,
            _ => ApiProjectVisibility.Private
        };
    }

    private static DomainProjectState MapState(ApiProjectState? state)
    {
        return state switch
        {
            ApiProjectState.New => DomainProjectState.New,
            ApiProjectState.WellFormed => DomainProjectState.WellFormed,
            ApiProjectState.CreatePending => DomainProjectState.CreatePending,
            ApiProjectState.Deleting => DomainProjectState.Deleting,
            ApiProjectState.Deleted => DomainProjectState.Deleted,
            _ => DomainProjectState.Unchanged
        };
    }

    private async Task<TeamProject> WaitForProjectCreation(
        ProjectHttpClient client,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        var maxAttempts = 30;
        var delayMs = 2000;

        for (var i = 0; i < maxAttempts; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            // ProjectHttpClient doesn't have GetOperation method
            // We need to poll the project until it's created
            await Task.Delay(delayMs, cancellationToken);
            
            // Try to get the project by ID from the operation result
            try
            {
                var createdProject = await client.GetProject(operationId.ToString());
                if (createdProject != null)
                {
                    return createdProject;
                }
            }
            catch
            {
                // Project not ready yet
            }
        }

        throw new TimeoutException("Project creation timed out");
    }
}