using DevOpsMcp.Contracts.Projects;

namespace DevOpsMcp.Application.Queries.Projects;

public sealed record GetProjectsQuery : IRequest<ErrorOr<List<ProjectDto>>>;

public sealed class GetProjectsQueryHandler(
    IProjectRepository projectRepository,
    IMemoryCache cache,
    ILogger<GetProjectsQueryHandler> logger)
    : IRequestHandler<GetProjectsQuery, ErrorOr<List<ProjectDto>>>
{
    public async Task<ErrorOr<List<ProjectDto>>> Handle(GetProjectsQuery request, CancellationToken cancellationToken)
    {
        const string cacheKey = "projects:all";
        
        if (cache.TryGetValue<List<ProjectDto>>(cacheKey, out var cachedProjects))
        {
            logger.LogDebug("Returning cached projects");
            return cachedProjects!;
        }

        logger.LogInformation("Fetching all projects");
        
        var projects = await projectRepository.GetAllAsync(cancellationToken);
        
        var projectDtos = projects.Select(p => new ProjectDto
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            OrganizationUrl = p.OrganizationUrl,
            Visibility = p.Visibility.ToString(),
            State = p.State.ToString(),
            CreatedDate = p.CreatedDate,
            LastUpdateTime = p.LastUpdateTime,
            Properties = p.Properties
        }).ToList();

        cache.Set(cacheKey, projectDtos, TimeSpan.FromMinutes(5));

        return projectDtos;
    }
}