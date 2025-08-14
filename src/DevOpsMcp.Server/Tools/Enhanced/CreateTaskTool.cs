using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DevOpsMcp.Domain.Entities;
using DevOpsMcp.Domain.Interfaces;
using DevOpsMcp.Server.Mcp;

namespace DevOpsMcp.Server.Tools.Enhanced;

public sealed class CreateTaskTool : BaseTool<CreateTaskArguments>
{
    private readonly IEnhancedTaskRepository _taskRepository;
    private readonly IEnhancedProjectRepository _projectRepository;
    
    public CreateTaskTool(
        IEnhancedTaskRepository taskRepository,
        IEnhancedProjectRepository projectRepository)
    {
        _taskRepository = taskRepository;
        _projectRepository = projectRepository;
    }
    
    public override string Name => "create_task";
    public override string Description => "Create a task in the enhanced task management system";
    public override JsonElement InputSchema => CreateSchema<CreateTaskArguments>();
    
    protected override async Task<CallToolResponse> ExecuteInternalAsync(
        CreateTaskArguments arguments, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate project exists
            if (!string.IsNullOrEmpty(arguments.ProjectId))
            {
                var project = await _projectRepository.GetByIdAsync(Guid.Parse(arguments.ProjectId));
                if (project == null)
                {
                    return CreateErrorResponse($"Project with ID {arguments.ProjectId} not found");
                }
            }
            
            var task = new DevOpsTask
            {
                ProjectId = !string.IsNullOrEmpty(arguments.ProjectId) 
                    ? Guid.Parse(arguments.ProjectId) 
                    : Guid.Empty,
                ParentTaskId = !string.IsNullOrEmpty(arguments.ParentTaskId) 
                    ? Guid.Parse(arguments.ParentTaskId) 
                    : null,
                Title = arguments.Title,
                Description = arguments.Description ?? string.Empty,
                Status = Enum.TryParse<DevOpsTaskStatus>(arguments.Status, true, out var status) 
                    ? status 
                    : DevOpsTaskStatus.Todo,
                Assignee = arguments.Assignee ?? "User",
                TaskOrder = arguments.TaskOrder ?? 0,
                Feature = arguments.Feature,
                Sources = JsonDocument.Parse(arguments.Sources ?? "[]"),
                CodeExamples = JsonDocument.Parse(arguments.CodeExamples ?? "[]")
            };
            
            var createdTask = await _taskRepository.CreateAsync(task);
            
            return CreateJsonResponse(new
            {
                task = new
                {
                    id = createdTask.Id,
                    projectId = createdTask.ProjectId,
                    parentTaskId = createdTask.ParentTaskId,
                    title = createdTask.Title,
                    description = createdTask.Description,
                    status = createdTask.Status.ToString().ToLowerInvariant(),
                    assignee = createdTask.Assignee,
                    taskOrder = createdTask.TaskOrder,
                    feature = createdTask.Feature,
                    createdAt = createdTask.CreatedAt,
                    updatedAt = createdTask.UpdatedAt
                },
                message = $"Task '{createdTask.Title}' created successfully"
            });
        }
        catch (Exception ex)
        {
            return CreateErrorResponse($"Failed to create task: {ex.Message}");
        }
    }
}

public class CreateTaskArguments
{
    public string ProjectId { get; set; } = string.Empty;
    public string? ParentTaskId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Status { get; set; }
    public string? Assignee { get; set; }
    public int? TaskOrder { get; set; }
    public string? Feature { get; set; }
    public string? Sources { get; set; } // JSON array string
    public string? CodeExamples { get; set; } // JSON array string
}