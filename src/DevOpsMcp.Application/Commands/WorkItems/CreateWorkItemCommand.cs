using DevOpsMcp.Contracts.WorkItems;

namespace DevOpsMcp.Application.Commands.WorkItems;

public sealed record CreateWorkItemCommand : IRequest<ErrorOr<WorkItemDto>>
{
    public required string ProjectId { get; init; }
    public required string WorkItemType { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public string? AssignedTo { get; init; }
    public required string AreaPath { get; init; }
    public required string IterationPath { get; init; }
    public int? Priority { get; init; }
    public string? Severity { get; init; }
    public IReadOnlyList<string>? Tags { get; init; }
    public Dictionary<string, object>? AdditionalFields { get; init; }
}

public sealed class CreateWorkItemCommandHandler : IRequestHandler<CreateWorkItemCommand, ErrorOr<WorkItemDto>>
{
    private readonly IWorkItemRepository _workItemRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IMediator _mediator;
    private readonly ILogger<CreateWorkItemCommandHandler> _logger;

    public CreateWorkItemCommandHandler(
        IWorkItemRepository workItemRepository,
        IProjectRepository projectRepository,
        IMediator mediator,
        ILogger<CreateWorkItemCommandHandler> logger)
    {
        _workItemRepository = workItemRepository;
        _projectRepository = projectRepository;
        _mediator = mediator;
        _logger = logger;
    }

    public async Task<ErrorOr<WorkItemDto>> Handle(CreateWorkItemCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating work item in project {ProjectId}", request.ProjectId);

        var projectExists = await _projectRepository.ExistsAsync(request.ProjectId, cancellationToken);
        if (!projectExists)
        {
            return Error.NotFound("Project.NotFound", $"Project {request.ProjectId} not found");
        }

        var workItem = WorkItem.Create(
            request.WorkItemType,
            request.Title,
            request.AreaPath,
            request.IterationPath,
            "System");

        workItem = workItem with
        {
            Description = request.Description,
            AssignedTo = request.AssignedTo,
            Priority = request.Priority,
            Severity = request.Severity,
            Tags = request.Tags ?? new List<string>()
        };

        if (request.AdditionalFields != null)
        {
            foreach (var field in request.AdditionalFields)
            {
                workItem.Fields[field.Key] = field.Value;
            }
        }

        var createdWorkItem = await _workItemRepository.CreateAsync(request.ProjectId, workItem, cancellationToken);

        await _mediator.Publish(new WorkItemCreatedEvent
        {
            ProjectId = request.ProjectId,
            WorkItemId = createdWorkItem.Id,
            WorkItemType = createdWorkItem.WorkItemType,
            Title = createdWorkItem.Title,
            CreatedBy = createdWorkItem.CreatedBy,
            CreatedDate = createdWorkItem.CreatedDate
        }, cancellationToken);

        return MapToDto(createdWorkItem);
    }

    private static WorkItemDto MapToDto(WorkItem workItem)
    {
        return new WorkItemDto
        {
            Id = workItem.Id,
            WorkItemType = workItem.WorkItemType,
            Title = workItem.Title,
            Description = workItem.Description,
            State = workItem.State.ToString(),
            AssignedTo = workItem.AssignedTo,
            AreaPath = workItem.AreaPath,
            IterationPath = workItem.IterationPath,
            CreatedDate = workItem.CreatedDate,
            CreatedBy = workItem.CreatedBy,
            ChangedDate = workItem.ChangedDate,
            ChangedBy = workItem.ChangedBy,
            Priority = workItem.Priority,
            Severity = workItem.Severity,
            Tags = workItem.Tags,
            Relations = workItem.Relations.Select(r => new WorkItemRelationDto
            {
                RelationType = r.RelationType,
                TargetUrl = r.TargetUrl,
                TargetId = r.TargetId,
                Attributes = r.Attributes
            }).ToList(),
            Fields = workItem.Fields
        };
    }
}