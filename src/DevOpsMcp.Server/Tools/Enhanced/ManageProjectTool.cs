using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DevOpsMcp.Domain.Entities.Enhanced;
using DevOpsMcp.Domain.Interfaces;
using DevOpsMcp.Server.Mcp;

namespace DevOpsMcp.Server.Tools.Enhanced;

public sealed class ManageProjectTool : BaseTool<ManageProjectArguments>
{
    private readonly IEnhancedProjectRepository _projectRepository;
    
    public ManageProjectTool(IEnhancedProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }
    
    public override string Name => "manage_project";
    public override string Description => "Manage enhanced projects (create, list, get, update, delete)";
    public override JsonElement InputSchema => CreateSchema<ManageProjectArguments>();
    
    protected override async Task<CallToolResponse> ExecuteInternalAsync(
        ManageProjectArguments arguments, 
        CancellationToken cancellationToken)
    {
        try
        {
            switch (arguments.Action.ToLowerInvariant())
            {
                case "create":
                    return await CreateProject(arguments);
                    
                case "list":
                    return await ListProjects();
                    
                case "get":
                    return await GetProject(arguments);
                    
                case "update":
                    return await UpdateProject(arguments);
                    
                case "delete":
                    return await DeleteProject(arguments);
                    
                default:
                    return CreateErrorResponse($"Unknown action: {arguments.Action}. Valid actions are: create, list, get, update, delete");
            }
        }
        catch (Exception ex)
        {
            return CreateErrorResponse($"Failed to {arguments.Action} project: {ex.Message}");
        }
    }
    
    private async Task<CallToolResponse> CreateProject(ManageProjectArguments arguments)
    {
        if (string.IsNullOrEmpty(arguments.Title))
            return CreateErrorResponse("Title is required for creating a project");
            
        var project = new EnhancedProject
        {
            Title = arguments.Title,
            Description = arguments.Description ?? string.Empty,
            GithubRepo = arguments.GithubRepo,
            Docs = !string.IsNullOrEmpty(arguments.Docs) 
                ? JsonDocument.Parse(arguments.Docs) 
                : JsonDocument.Parse("[]"),
            Features = !string.IsNullOrEmpty(arguments.Features) 
                ? JsonDocument.Parse(arguments.Features) 
                : JsonDocument.Parse("[]"),
            Data = !string.IsNullOrEmpty(arguments.Data) 
                ? JsonDocument.Parse(arguments.Data) 
                : JsonDocument.Parse("[]"),
            Pinned = arguments.Pinned ?? false
        };
        
        var created = await _projectRepository.CreateAsync(project);
        
        return CreateJsonResponse(new
        {
            project = FormatProject(created),
            message = $"Project '{created.Title}' created successfully"
        });
    }
    
    private async Task<CallToolResponse> ListProjects()
    {
        var projects = await _projectRepository.ListAsync();
        
        return CreateJsonResponse(new
        {
            projects = projects.Select(FormatProject),
            count = projects.Count
        });
    }
    
    private async Task<CallToolResponse> GetProject(ManageProjectArguments arguments)
    {
        if (string.IsNullOrEmpty(arguments.ProjectId))
            return CreateErrorResponse("ProjectId is required for getting a project");
            
        var project = await _projectRepository.GetByIdAsync(Guid.Parse(arguments.ProjectId));
        if (project == null)
            return CreateErrorResponse($"Project with ID {arguments.ProjectId} not found");
            
        return CreateJsonResponse(new
        {
            project = FormatProject(project)
        });
    }
    
    private async Task<CallToolResponse> UpdateProject(ManageProjectArguments arguments)
    {
        if (string.IsNullOrEmpty(arguments.ProjectId))
            return CreateErrorResponse("ProjectId is required for updating a project");
            
        var project = await _projectRepository.GetByIdAsync(Guid.Parse(arguments.ProjectId));
        if (project == null)
            return CreateErrorResponse($"Project with ID {arguments.ProjectId} not found");
            
        // Update only provided fields
        if (!string.IsNullOrEmpty(arguments.Title))
            project.Title = arguments.Title;
            
        if (arguments.Description != null)
            project.Description = arguments.Description;
            
        if (arguments.GithubRepo != null)
            project.GithubRepo = string.IsNullOrEmpty(arguments.GithubRepo) ? null : arguments.GithubRepo;
            
        if (!string.IsNullOrEmpty(arguments.Docs))
            project.Docs = JsonDocument.Parse(arguments.Docs);
            
        if (!string.IsNullOrEmpty(arguments.Features))
            project.Features = JsonDocument.Parse(arguments.Features);
            
        if (!string.IsNullOrEmpty(arguments.Data))
            project.Data = JsonDocument.Parse(arguments.Data);
            
        if (arguments.Pinned.HasValue)
            project.Pinned = arguments.Pinned.Value;
            
        var updated = await _projectRepository.UpdateAsync(project);
        
        return CreateJsonResponse(new
        {
            project = FormatProject(updated),
            message = $"Project '{updated.Title}' updated successfully"
        });
    }
    
    private async Task<CallToolResponse> DeleteProject(ManageProjectArguments arguments)
    {
        if (string.IsNullOrEmpty(arguments.ProjectId))
            return CreateErrorResponse("ProjectId is required for deleting a project");
            
        var success = await _projectRepository.DeleteAsync(Guid.Parse(arguments.ProjectId));
        if (!success)
            return CreateErrorResponse($"Failed to delete project with ID {arguments.ProjectId}");
            
        return CreateJsonResponse(new
        {
            message = "Project deleted successfully",
            projectId = arguments.ProjectId
        });
    }
    
    private object FormatProject(EnhancedProject project)
    {
        return new
        {
            id = project.Id,
            title = project.Title,
            description = project.Description,
            githubRepo = project.GithubRepo,
            docs = JsonSerializer.Deserialize<object>(project.Docs.RootElement.GetRawText()),
            features = JsonSerializer.Deserialize<object>(project.Features.RootElement.GetRawText()),
            data = JsonSerializer.Deserialize<object>(project.Data.RootElement.GetRawText()),
            pinned = project.Pinned,
            createdAt = project.CreatedAt,
            updatedAt = project.UpdatedAt
        };
    }
}

public class ManageProjectArguments
{
    public string Action { get; set; } = string.Empty; // create, list, get, update, delete
    public string? ProjectId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? GithubRepo { get; set; }
    public string? Docs { get; set; } // JSON array string
    public string? Features { get; set; } // JSON array string
    public string? Data { get; set; } // JSON array string
    public bool? Pinned { get; set; }
}