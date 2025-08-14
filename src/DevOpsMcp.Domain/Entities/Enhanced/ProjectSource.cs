using System;

namespace DevOpsMcp.Domain.Entities.Enhanced;

/// <summary>
/// Junction entity linking projects to knowledge sources
/// Maps to archon_project_sources table in the shared database
/// </summary>
public class ProjectSource
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string SourceId { get; set; } = string.Empty;
    public DateTime LinkedAt { get; set; }
    public string CreatedBy { get; set; } = "system";
    public string? Notes { get; set; }
    
    // Navigation
    public EnhancedProject? Project { get; set; }
    public KnowledgeSource? Source { get; set; }
}