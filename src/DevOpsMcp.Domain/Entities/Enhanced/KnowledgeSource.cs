using System;
using System.Collections.Generic;
using System.Text.Json;

namespace DevOpsMcp.Domain.Entities.Enhanced;

/// <summary>
/// Knowledge source entity for organizing documentation
/// Maps to archon_sources table in the shared database
/// </summary>
public class KnowledgeSource
{
    public string SourceId { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public int TotalWordCount { get; set; }
    public string? Title { get; set; }
    public JsonDocument Metadata { get; set; } = JsonDocument.Parse("{}");
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation
    public List<KnowledgeDocument> Documents { get; } = new();
    public List<ProjectSource> ProjectSources { get; } = new();
}