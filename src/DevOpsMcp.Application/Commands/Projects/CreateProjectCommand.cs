using DevOpsMcp.Contracts.Projects;

namespace DevOpsMcp.Application.Commands.Projects;

public sealed record CreateProjectCommand : IRequest<ErrorOr<ProjectDto>>
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string OrganizationUrl { get; init; }
    public string Visibility { get; init; } = "Private";
    public Dictionary<string, object>? Properties { get; init; }
}

public sealed class CreateProjectCommandHandler(
    IProjectRepository projectRepository,
    ILogger<CreateProjectCommandHandler> logger)
    : IRequestHandler<CreateProjectCommand, ErrorOr<ProjectDto>>
{
    public async Task<ErrorOr<ProjectDto>> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating project {ProjectName}", request.Name);

        var organizationUrl = OrganizationUrl.Create(request.OrganizationUrl);
        if (organizationUrl.IsError)
        {
            return organizationUrl.Errors;
        }

        if (!Enum.TryParse<ProjectVisibility>(request.Visibility, out var visibility))
        {
            return Error.Validation("Project.InvalidVisibility", $"Invalid visibility: {request.Visibility}");
        }

        var projectId = Guid.NewGuid().ToString();
        var project = Project.Create(
            projectId,
            request.Name,
            request.Description,
            organizationUrl.Value,
            visibility);

        if (request.Properties != null)
        {
            foreach (var property in request.Properties)
            {
                project.Properties[property.Key] = property.Value;
            }
        }

        var createdProject = await projectRepository.CreateAsync(project, cancellationToken);

        return new ProjectDto
        {
            Id = createdProject.Id,
            Name = createdProject.Name,
            Description = createdProject.Description,
            OrganizationUrl = createdProject.OrganizationUrl,
            Visibility = createdProject.Visibility.ToString(),
            State = createdProject.State.ToString(),
            CreatedDate = createdProject.CreatedDate,
            LastUpdateTime = createdProject.LastUpdateTime,
            Properties = createdProject.Properties
        };
    }
}