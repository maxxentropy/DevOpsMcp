using DevOpsMcp.Contracts.WorkItems;
using DevOpsMcp.Domain.Entities;
using DevOpsMcp.Application.Validators;

namespace DevOpsMcp.Application.Queries.WorkItems;

public sealed record QueryWorkItemsQuery : IRequest<ErrorOr<List<WorkItemDto>>>
{
    public required string ProjectId { get; init; }
    public required string Wiql { get; init; }
    public int Limit { get; init; } = 50;
    public int Skip { get; init; }
    public IReadOnlyList<string>? Fields { get; init; }
    public bool IncludeRelations { get; init; }
}

public sealed class QueryWorkItemsQueryHandler(
    IWorkItemRepository workItemRepository,
    IProjectRepository projectRepository,
    ILogger<QueryWorkItemsQueryHandler> logger)
    : IRequestHandler<QueryWorkItemsQuery, ErrorOr<List<WorkItemDto>>>
{
    public async Task<ErrorOr<List<WorkItemDto>>> Handle(QueryWorkItemsQuery request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Querying work items in project {ProjectId} with limit {Limit}", 
            request.ProjectId, request.Limit);

        // Validate WIQL query
        var validation = WiqlValidator.Validate(request.Wiql, request.ProjectId);
        if (!validation.IsValid)
        {
            return Error.Validation("Wiql.Invalid", validation.Message!);
        }
        
        if (validation.IsWarning)
        {
            logger.LogWarning("WIQL validation warning: {Warning}", validation.Message);
        }

        var projectExists = await projectRepository.ExistsAsync(request.ProjectId, cancellationToken);
        if (!projectExists)
        {
            return Error.NotFound("Project.NotFound", $"Project {request.ProjectId} not found");
        }

        var options = new WorkItemQueryOptions
        {
            Limit = request.Limit,
            Skip = request.Skip,
            Fields = request.Fields,
            IncludeRelations = request.IncludeRelations
        };

        try
        {
            var workItems = await workItemRepository.QueryAsync(request.ProjectId, request.Wiql, options, cancellationToken);

            var dtos = workItems.Select(MapToDto).ToList();
            return dtos;
        }
        catch (Exception ex) when (ex.Message.Contains("VS403437") || ex.Message.Contains("FROM clause"))
        {
            // Azure DevOps error for TOP clause
            return Error.Validation("Wiql.TopNotSupported", 
                "The TOP clause is not supported in Azure DevOps WIQL. " +
                "Use the 'limit' parameter instead to control the number of results.");
        }
        catch (Exception ex) when (ex.Message.Contains("syntax"))
        {
            return Error.Validation("Wiql.SyntaxError", 
                $"WIQL syntax error: {ex.Message}. " +
                $"Example valid query: {WiqlValidator.GetSampleQuery()}");
        }
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