using System;
using System.Collections.Generic;
using System.Text.Json;

namespace DevOpsMcp.Domain.Entities;

/// <summary>
/// Enhanced task entity that integrates with shared task management system
/// Maps to archon_tasks table in the shared database
/// </summary>
public class DevOpsTask
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? ParentTaskId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DevOpsTaskStatus Status { get; set; } = DevOpsTaskStatus.Todo;
    public string Assignee { get; set; } = "User";
    public int TaskOrder { get; set; }
    public string? Feature { get; set; }
    
    // JSONB fields for flexible metadata storage
    public JsonDocument Sources { get; set; } = JsonDocument.Parse("[]");
    public JsonDocument CodeExamples { get; set; } = JsonDocument.Parse("[]");
    
    // Soft delete support
    public bool Archived { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public string? ArchivedBy { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public Enhanced.EnhancedProject? Project { get; set; }
    public DevOpsTask? ParentTask { get; set; }
    public List<DevOpsTask> SubTasks { get; } = new();
}

public enum DevOpsTaskStatus
{
    Todo,
    Doing,
    Review,
    Done
}