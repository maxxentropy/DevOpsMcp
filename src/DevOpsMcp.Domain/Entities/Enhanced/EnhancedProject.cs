using System;
using System.Collections.Generic;
using System.Text.Json;

namespace DevOpsMcp.Domain.Entities.Enhanced;

/// <summary>
/// Enhanced project entity that provides cross-platform project management
/// Maps to archon_projects table in the shared database
/// </summary>
public class EnhancedProject
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? GithubRepo { get; set; }
    
    // JSONB fields for flexible document storage
    public JsonDocument Docs { get; set; } = JsonDocument.Parse("[]");
    public JsonDocument Features { get; set; } = JsonDocument.Parse("[]");
    public JsonDocument Data { get; set; } = JsonDocument.Parse("[]");
    
    public bool Pinned { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public List<DevOpsTask> Tasks { get; } = new();
    public List<ProjectSource> ProjectSources { get; } = new();
    public List<DocumentVersion> DocumentVersions { get; } = new();
}