using System;
using System.Text.Json;

namespace DevOpsMcp.Domain.Entities.Enhanced;

/// <summary>
/// Document version entity for version control
/// Maps to archon_document_versions table in the shared database
/// </summary>
public class DocumentVersion
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? TaskId { get; set; } // Deprecated, kept for compatibility
    public string FieldName { get; set; } = string.Empty; // 'docs', 'features', 'data', 'prd'
    public int VersionNumber { get; set; }
    public JsonDocument Content { get; set; } = JsonDocument.Parse("{}");
    public string? ChangeSummary { get; set; }
    public string ChangeType { get; set; } = "update"; // 'create', 'update', 'delete', 'restore', 'backup'
    public string? DocumentId { get; set; } // For docs array, specific document ID
    public string CreatedBy { get; set; } = "system";
    public DateTime CreatedAt { get; set; }
    
    // Navigation
    public EnhancedProject? Project { get; set; }
}