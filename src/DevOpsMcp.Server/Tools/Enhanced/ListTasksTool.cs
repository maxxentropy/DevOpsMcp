using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DevOpsMcp.Domain.Entities;
using DevOpsMcp.Domain.Interfaces;
using DevOpsMcp.Server.Mcp;

namespace DevOpsMcp.Server.Tools.Enhanced;

public sealed class ListTasksTool : BaseTool<ListTasksArguments>
{
    private readonly IEnhancedTaskRepository _taskRepository;
    
    public ListTasksTool(IEnhancedTaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }
    
    public override string Name => "list_tasks";
    public override string Description => "List tasks with filtering and sorting options";
    public override JsonElement InputSchema => CreateSchema<ListTasksArguments>();
    
    protected override async Task<CallToolResponse> ExecuteInternalAsync(
        ListTasksArguments arguments, 
        CancellationToken cancellationToken)
    {
        try
        {
            var filter = new TaskFilter
            {
                ProjectId = arguments.ProjectId,
                Status = Enum.TryParse<DevOpsTaskStatus>(arguments.Status, true, out var status) 
                    ? status 
                    : null,
                Assignee = arguments.Assignee,
                Feature = arguments.Feature,
                IncludeDone = arguments.IncludeArchived ?? false,
                Skip = arguments.Skip ?? 0,
                Take = arguments.Take ?? 50,
                SortBy = Enum.TryParse<TaskSortBy>(arguments.SortBy, true, out var sortBy) 
                    ? sortBy 
                    : TaskSortBy.Priority,
                SortDescending = arguments.SortDescending ?? true
            };
            
            var tasks = await _taskRepository.ListAsync(filter);
            var totalCount = await _taskRepository.CountAsync(filter);
            
            return CreateJsonResponse(new
            {
                tasks = tasks.Select(t => new
                {
                    id = t.Id,
                    projectId = t.ProjectId,
                    parentTaskId = t.ParentTaskId,
                    title = t.Title,
                    description = t.Description,
                    status = t.Status.ToString().ToLowerInvariant(),
                    assignee = t.Assignee,
                    taskOrder = t.TaskOrder,
                    feature = t.Feature,
                    archived = t.Archived,
                    createdAt = t.CreatedAt,
                    updatedAt = t.UpdatedAt
                }),
                pagination = new
                {
                    total = totalCount,
                    skip = filter.Skip,
                    take = filter.Take,
                    hasMore = totalCount > filter.Skip + filter.Take
                }
            });
        }
        catch (Exception ex)
        {
            return CreateErrorResponse($"Failed to list tasks: {ex.Message}");
        }
    }
}

public class ListTasksArguments
{
    public string? ProjectId { get; set; }
    public string? Status { get; set; }
    public string? Assignee { get; set; }
    public string? Feature { get; set; }
    public bool? IncludeArchived { get; set; }
    public int? Skip { get; set; }
    public int? Take { get; set; }
    public string? SortBy { get; set; }
    public bool? SortDescending { get; set; }
}