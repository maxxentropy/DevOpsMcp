using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DevOpsMcp.Domain.Entities.Enhanced;
using DevOpsMcp.Domain.Interfaces;
using DevOpsMcp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Pgvector;

namespace DevOpsMcp.Infrastructure.Repositories.Enhanced;

public class KnowledgeRepository : IKnowledgeRepository
{
    private readonly EnhancedFeaturesDbContext _context;
    private readonly Supabase.Client? _supabaseClient;
    
    public KnowledgeRepository(EnhancedFeaturesDbContext context, Supabase.Client? supabaseClient = null)
    {
        _context = context;
        _supabaseClient = supabaseClient;
    }
    
    public async Task<List<KnowledgeSearchResult>> SearchAsync(
        IReadOnlyList<float> queryEmbedding, 
        int matchCount = 10, 
        string? sourceFilter = null)
    {
        // Convert IReadOnlyList to pgvector
        var vector = new Vector(queryEmbedding as float[] ?? queryEmbedding.ToArray());
        
        // Using raw SQL for vector similarity search
        var sql = @"
            SELECT 
                id,
                url,
                chunk_number,
                content,
                source_id,
                1 - (embedding <=> @embedding) AS similarity
            FROM archon_crawled_pages
            WHERE (@sourceFilter IS NULL OR source_id = @sourceFilter)
            ORDER BY embedding <=> @embedding
            LIMIT @matchCount";
            
        var parameters = new object[] { vector, sourceFilter ?? (object)DBNull.Value, matchCount };
        var results = await _context.KnowledgeDocuments
            .FromSqlRaw(sql, parameters)
            .Select(d => new KnowledgeSearchResult
            {
                Id = d.Id,
                Url = d.Url,
                ChunkNumber = d.ChunkNumber,
                Content = d.Content,
                SourceId = d.SourceId,
                Similarity = 0 // Would be calculated in SQL
            })
            .ToListAsync();
            
        return results;
    }
    
    public async Task<List<KnowledgeSearchResult>> SearchCodeExamplesAsync(
        IReadOnlyList<float> queryEmbedding, 
        int matchCount = 5, 
        string? sourceFilter = null)
    {
        // This would search the code examples table
        // For now, return empty list as code examples table isn't mapped yet
        return new List<KnowledgeSearchResult>();
    }
    
    public async Task<KnowledgeDocument> AddDocumentAsync(KnowledgeDocument document)
    {
        document.CreatedAt = System.DateTime.UtcNow;
        _context.KnowledgeDocuments.Add(document);
        await _context.SaveChangesAsync();
        
        return document;
    }
    
    public async Task<int> AddDocumentBatchAsync(List<KnowledgeDocument> documents)
    {
        foreach (var doc in documents)
        {
            doc.CreatedAt = System.DateTime.UtcNow;
        }
        
        _context.KnowledgeDocuments.AddRange(documents);
        return await _context.SaveChangesAsync();
    }
    
    public async Task<bool> DeleteByUrlAsync(string url)
    {
        var documents = await _context.KnowledgeDocuments
            .Where(d => d.Url == url)
            .ToListAsync();
            
        if (!documents.Any())
            return false;
            
        _context.KnowledgeDocuments.RemoveRange(documents);
        await _context.SaveChangesAsync();
        
        return true;
    }
    
    public async Task<List<KnowledgeSource>> ListSourcesAsync()
    {
        return await _context.KnowledgeSources
            .OrderBy(s => s.Title)
            .ToListAsync();
    }
}