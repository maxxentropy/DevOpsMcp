using DevOpsMcp.Contracts.Projects;

namespace DevOpsMcp.Application.Queries.Projects;

public sealed record GetProjectsQuery : IRequest<ErrorOr<List<ProjectDto>>>;

public sealed class GetProjectsQueryHandler : IRequestHandler<GetProjectsQuery, ErrorOr<List<ProjectDto>>>
{
    private readonly IProjectRepository _projectRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<GetProjectsQueryHandler> _logger;

    public GetProjectsQueryHandler(
        IProjectRepository projectRepository,
        IMemoryCache cache,
        ILogger<GetProjectsQueryHandler> logger)
    {
        _projectRepository = projectRepository;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ErrorOr<List<ProjectDto>>> Handle(GetProjectsQuery request, CancellationToken cancellationToken)
    {
        const string cacheKey = "projects:all";
        
        if (_cache.TryGetValue<List<ProjectDto>>(cacheKey, out var cachedProjects))
        {
            _logger.LogDebug("Returning cached projects");
            return cachedProjects!;
        }

        _logger.LogInformation("Fetching all projects");
        
        var projects = await _projectRepository.GetAllAsync(cancellationToken);
        
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

        _cache.Set(cacheKey, projectDtos, TimeSpan.FromMinutes(5));

        return projectDtos;
    }
}