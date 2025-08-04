using System.ComponentModel;
using System.Text.Json;
using MediatR;
using DevOpsMcp.Application.Queries.WorkItems;
using DevOpsMcp.Domain.Entities;
using DevOpsMcp.Server.Mcp;

namespace DevOpsMcp.Server.Tools.WorkItems;

/// <summary>
/// Simplified tool for getting recent work items without writing WIQL
/// </summary>
public sealed class GetRecentWorkItemsTool(IMediator mediator) : BaseTool<GetRecentWorkItemsToolArguments>
{
    public override string Name => "get_recent_work_items";
    
    public override string Description => @"Get recently updated work items from a project. 
This is a simplified alternative to query_work_items that doesn't require WIQL knowledge.
Returns work items ordered by last changed date (newest first).";
    
    public override JsonElement InputSchema => CreateSchema<GetRecentWorkItemsToolArguments>();

    protected override async Task<CallToolResponse> ExecuteInternalAsync(
        GetRecentWorkItemsToolArguments arguments, 
        CancellationToken cancellationToken)
    {
        // Build WIQL query based on parameters
        var conditions = new List<string>
        {
            $"[System.TeamProject] = '{arguments.ProjectId}'"
        };

        if (!string.IsNullOrEmpty(arguments.WorkItemType))
        {
            conditions.Add($"[System.WorkItemType] = '{arguments.WorkItemType}'");
        }

        if (!string.IsNullOrEmpty(arguments.State))
        {
            conditions.Add($"[System.State] = '{arguments.State}'");
        }

        if (!string.IsNullOrEmpty(arguments.AssignedTo))
        {
            conditions.Add($"[System.AssignedTo] = '{arguments.AssignedTo}'");
        }

        var whereClause = string.Join(" AND ", conditions);
        var wiql = $"SELECT [System.Id] FROM WorkItems WHERE {whereClause} ORDER BY [System.ChangedDate] DESC";

        var query = new QueryWorkItemsQuery
        {
            ProjectId = arguments.ProjectId,
            Wiql = wiql,
            Limit = arguments.Count,
            Skip = 0,
            Fields = arguments.IncludeDetails ? null : WorkItemQueryOptions.DefaultFields,
            IncludeRelations = false
        };

        var result = await mediator.Send(query, cancellationToken);

        if (result.IsError)
        {
            return CreateErrorResponse($"Failed to get recent work items: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return CreateJsonResponse(new
        {
            workItems = result.Value,
            count = result.Value.Count,
            generatedQuery = wiql
        });
    }
}

public sealed record GetRecentWorkItemsToolArguments
{
    [Description("The project ID to get work items from")]
    public required string ProjectId { get; init; }
    
    [Description("Filter by work item type (e.g., 'Bug', 'Task', 'User Story')")]
    public string? WorkItemType { get; init; }
    
    [Description("Filter by state (e.g., 'Active', 'Resolved', 'Closed')")]
    public string? State { get; init; }
    
    [Description("Filter by assigned to (use '@Me' for current user)")]
    public string? AssignedTo { get; init; }
    
    [Description("Number of work items to return (default: 10, max: 50)")]
    public int Count { get; init; } = 10;
    
    [Description("Include all fields in response (default: false, returns only basic fields)")]
    public bool IncludeDetails { get; init; }
}