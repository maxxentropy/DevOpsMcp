using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DevOpsMcp.Domain.Entities;
using DevOpsMcp.Domain.Interfaces;
using DevOpsMcp.Server.Mcp;

namespace DevOpsMcp.Server.Tools.Enhanced;

public sealed class UpdateTaskTool : BaseTool<UpdateTaskArguments>
{
    private readonly IEnhancedTaskRepository _taskRepository;
    
    public UpdateTaskTool(IEnhancedTaskRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }
    
    public override string Name => "update_task";
    public override string Description => "Update an existing task's properties";
    public override JsonElement InputSchema => CreateSchema<UpdateTaskArguments>();
    
    protected override async Task<CallToolResponse> ExecuteInternalAsync(
        UpdateTaskArguments arguments, 
        CancellationToken cancellationToken)
    {
        try
        {
            var task = await _taskRepository.GetByIdAsync(Guid.Parse(arguments.TaskId));
            if (task == null)
            {
                return CreateErrorResponse($"Task with ID {arguments.TaskId} not found");
            }
            
            // Update only provided fields
            if (!string.IsNullOrEmpty(arguments.Title))
                task.Title = arguments.Title;
                
            if (!string.IsNullOrEmpty(arguments.Description))
                task.Description = arguments.Description;
                
            if (!string.IsNullOrEmpty(arguments.Status) && 
                Enum.TryParse<DevOpsTaskStatus>(arguments.Status, true, out var status))
                task.Status = status;
                
            if (!string.IsNullOrEmpty(arguments.Assignee))
                task.Assignee = arguments.Assignee;
                
            if (arguments.TaskOrder.HasValue)
                task.TaskOrder = arguments.TaskOrder.Value;
                
            if (arguments.Feature != null) // Allow clearing with empty string
                task.Feature = string.IsNullOrEmpty(arguments.Feature) ? null : arguments.Feature;
                
            if (!string.IsNullOrEmpty(arguments.Sources))
                task.Sources = JsonDocument.Parse(arguments.Sources);
                
            if (!string.IsNullOrEmpty(arguments.CodeExamples))
                task.CodeExamples = JsonDocument.Parse(arguments.CodeExamples);
            
            var updatedTask = await _taskRepository.UpdateAsync(task);
            
            return CreateJsonResponse(new
            {
                task = new
                {
                    id = updatedTask.Id,
                    projectId = updatedTask.ProjectId,
                    parentTaskId = updatedTask.ParentTaskId,
                    title = updatedTask.Title,
                    description = updatedTask.Description,
                    status = updatedTask.Status.ToString().ToLowerInvariant(),
                    assignee = updatedTask.Assignee,
                    taskOrder = updatedTask.TaskOrder,
                    feature = updatedTask.Feature,
                    archived = updatedTask.Archived,
                    updatedAt = updatedTask.UpdatedAt
                },
                message = $"Task '{updatedTask.Title}' updated successfully"
            });
        }
        catch (Exception ex)
        {
            return CreateErrorResponse($"Failed to update task: {ex.Message}");
        }
    }
}

public class UpdateTaskArguments
{
    public string TaskId { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Status { get; set; }
    public string? Assignee { get; set; }
    public int? TaskOrder { get; set; }
    public string? Feature { get; set; }
    public string? Sources { get; set; } // JSON array string
    public string? CodeExamples { get; set; } // JSON array string
}