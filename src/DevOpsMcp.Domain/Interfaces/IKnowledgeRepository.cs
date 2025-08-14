using System.Collections.Generic;
using System.Threading.Tasks;
using DevOpsMcp.Domain.Entities.Enhanced;

namespace DevOpsMcp.Domain.Interfaces;

public interface IKnowledgeRepository
{
    Task<List<KnowledgeSearchResult>> SearchAsync(IReadOnlyList<float> queryEmbedding, int matchCount = 10, string? sourceFilter = null);
    Task<List<KnowledgeSearchResult>> SearchCodeExamplesAsync(IReadOnlyList<float> queryEmbedding, int matchCount = 5, string? sourceFilter = null);
    Task<KnowledgeDocument> AddDocumentAsync(KnowledgeDocument document);
    Task<int> AddDocumentBatchAsync(List<KnowledgeDocument> documents);
    Task<bool> DeleteByUrlAsync(string url);
    Task<List<KnowledgeSource>> ListSourcesAsync();
}

public class KnowledgeSearchResult
{
    public long Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public int ChunkNumber { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public string SourceId { get; set; } = string.Empty;
    public float Similarity { get; set; }
}