using System;
using System.Text.Json;

namespace DevOpsMcp.Domain.Entities.Enhanced;

/// <summary>
/// Knowledge document entity for RAG capabilities
/// Maps to archon_crawled_pages table in the shared database
/// </summary>
public class KnowledgeDocument
{
    public long Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public int ChunkNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public JsonDocument Metadata { get; set; } = JsonDocument.Parse("{}");
    public string SourceId { get; set; } = string.Empty;
    
    // Vector embedding for semantic search - stored as IReadOnlyList in domain
    // Will be converted to pgvector type in infrastructure layer
    public IReadOnlyList<float>? Embedding { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    // Navigation
    public KnowledgeSource? Source { get; set; }
}