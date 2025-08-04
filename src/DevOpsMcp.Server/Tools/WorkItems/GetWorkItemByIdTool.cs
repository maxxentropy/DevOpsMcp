using System.ComponentModel;
using System.Text.Json;
using MediatR;
using DevOpsMcp.Application.Queries.WorkItems;
using DevOpsMcp.Domain.Interfaces;
using DevOpsMcp.Server.Mcp;

namespace DevOpsMcp.Server.Tools.WorkItems;

/// <summary>
/// Optimized tool for getting a single work item by ID
/// </summary>
public sealed class GetWorkItemByIdTool(
    IWorkItemRepository workItemRepository) : BaseTool<GetWorkItemByIdToolArguments>
{
    public override string Name => "get_work_item_by_id";
    
    public override string Description => @"Get a single work item by its ID. 
This is more efficient than using query_work_items for fetching individual items.
Returns the work item with all standard fields or specific fields if requested.";
    
    public override JsonElement InputSchema => CreateSchema<GetWorkItemByIdToolArguments>();

    protected override async Task<CallToolResponse> ExecuteInternalAsync(
        GetWorkItemByIdToolArguments arguments, 
        CancellationToken cancellationToken)
    {
        try
        {
            var workItem = await workItemRepository.GetByIdAsync(
                arguments.ProjectId, 
                arguments.WorkItemId, 
                cancellationToken);

            if (workItem == null)
            {
                return CreateErrorResponse($"Work item {arguments.WorkItemId} not found in project {arguments.ProjectId}");
            }

            // If specific fields are requested, create a filtered response
            if (arguments.Fields != null && arguments.Fields.Any())
            {
                var filteredWorkItem = new Dictionary<string, object?>
                {
                    ["id"] = workItem.Id,
                    ["url"] = $"https://dev.azure.com/{arguments.ProjectId}/_workitems/edit/{workItem.Id}"
                };

                // Map requested fields
                foreach (var field in arguments.Fields)
                {
                    var value = field.ToLowerInvariant() switch
                    {
                        "title" or "system.title" => workItem.Title,
                        "description" or "system.description" => workItem.Description,
                        "workitemtype" or "system.workitemtype" => workItem.WorkItemType,
                        "state" or "system.state" => workItem.State.ToString(),
                        "assignedto" or "system.assignedto" => workItem.AssignedTo,
                        "createddate" or "system.createddate" => workItem.CreatedDate,
                        "changeddate" or "system.changeddate" => workItem.ChangedDate,
                        "areapath" or "system.areapath" => workItem.AreaPath,
                        "iterationpath" or "system.iterationpath" => workItem.IterationPath,
                        "priority" or "microsoft.vsts.common.priority" => workItem.Priority,
                        "severity" or "microsoft.vsts.common.severity" => workItem.Severity,
                        "tags" or "system.tags" => string.Join(";", workItem.Tags),
                        _ => workItem.Fields.GetValueOrDefault(field)
                    };

                    if (value != null)
                    {
                        filteredWorkItem[field] = value;
                    }
                }

                return CreateJsonResponse(new
                {
                    workItem = filteredWorkItem,
                    projectId = arguments.ProjectId
                });
            }

            // Return full work item
            return CreateJsonResponse(new
            {
                workItem = new
                {
                    workItem.Id,
                    workItem.Title,
                    workItem.Description,
                    workItem.WorkItemType,
                    State = workItem.State.ToString(),
                    workItem.AssignedTo,
                    workItem.CreatedDate,
                    workItem.CreatedBy,
                    workItem.ChangedDate,
                    workItem.ChangedBy,
                    workItem.AreaPath,
                    workItem.IterationPath,
                    workItem.Priority,
                    workItem.Severity,
                    Tags = string.Join(";", workItem.Tags),
                    Url = $"https://dev.azure.com/{arguments.ProjectId}/_workitems/edit/{workItem.Id}",
                    Relations = arguments.IncludeRelations ? workItem.Relations : null,
                    Fields = arguments.IncludeAllFields ? workItem.Fields : null
                },
                projectId = arguments.ProjectId
            });
        }
        catch (Exception ex)
        {
            return CreateErrorResponse($"Failed to get work item: {ex.Message}");
        }
    }
}

public sealed record GetWorkItemByIdToolArguments
{
    [Description("The project ID containing the work item")]
    public required string ProjectId { get; init; }
    
    [Description("The ID of the work item to retrieve")]
    public required int WorkItemId { get; init; }
    
    [Description("Specific fields to return. If not specified, returns standard fields")]
    public IReadOnlyList<string>? Fields { get; init; }
    
    [Description("Include work item relations in response (default: false)")]
    public bool IncludeRelations { get; init; }
    
    [Description("Include all custom fields in response (default: false)")]
    public bool IncludeAllFields { get; init; }
}