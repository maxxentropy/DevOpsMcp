using DevOpsMcp.Contracts.WorkItems;

namespace DevOpsMcp.Application.Queries.WorkItems;

public sealed record QueryWorkItemsQuery : IRequest<ErrorOr<List<WorkItemDto>>>
{
    public required string ProjectId { get; init; }
    public required string Wiql { get; init; }
}

public sealed class QueryWorkItemsQueryHandler(
    IWorkItemRepository workItemRepository,
    IProjectRepository projectRepository,
    ILogger<QueryWorkItemsQueryHandler> logger)
    : IRequestHandler<QueryWorkItemsQuery, ErrorOr<List<WorkItemDto>>>
{
    public async Task<ErrorOr<List<WorkItemDto>>> Handle(QueryWorkItemsQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Querying work items in project {ProjectId}", request.ProjectId);

        var projectExists = await projectRepository.ExistsAsync(request.ProjectId, cancellationToken);
        if (!projectExists)
        {
            return Error.NotFound("Project.NotFound", $"Project {request.ProjectId} not found");
        }

        var workItems = await workItemRepository.QueryAsync(request.ProjectId, request.Wiql, cancellationToken);

        var dtos = workItems.Select(MapToDto).ToList();
        return dtos;
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