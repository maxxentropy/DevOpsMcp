using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DevOpsMcp.Domain.Entities.Enhanced;
using DevOpsMcp.Domain.Interfaces;
using DevOpsMcp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DevOpsMcp.Infrastructure.Repositories.Enhanced;

public class DocumentVersionRepository : IDocumentVersionRepository
{
    private readonly EnhancedFeaturesDbContext _context;
    
    public DocumentVersionRepository(EnhancedFeaturesDbContext context)
    {
        _context = context;
    }
    
    public async Task<DocumentVersion> CreateVersionAsync(DocumentVersion version)
    {
        version.Id = Guid.NewGuid();
        version.CreatedAt = DateTime.UtcNow;
        
        // Auto-increment version number
        version.VersionNumber = await GetNextVersionNumberAsync(version.ProjectId, version.FieldName);
        
        _context.DocumentVersions.Add(version);
        await _context.SaveChangesAsync();
        
        return version;
    }
    
    public async Task<List<DocumentVersion>> GetVersionHistoryAsync(Guid projectId, string fieldName)
    {
        return await _context.DocumentVersions
            .Where(v => v.ProjectId == projectId && v.FieldName == fieldName)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync();
    }
    
    public async Task<DocumentVersion?> GetVersionAsync(Guid projectId, string fieldName, int versionNumber)
    {
        return await _context.DocumentVersions
            .FirstOrDefaultAsync(v => 
                v.ProjectId == projectId && 
                v.FieldName == fieldName && 
                v.VersionNumber == versionNumber);
    }
    
    public async Task<DocumentVersion> RestoreVersionAsync(
        Guid projectId, 
        string fieldName, 
        int versionNumber, 
        string restoredBy)
    {
        var versionToRestore = await GetVersionAsync(projectId, fieldName, versionNumber);
        if (versionToRestore == null)
            throw new System.InvalidOperationException($"Version {versionNumber} not found");
            
        var restoredVersion = new DocumentVersion
        {
            ProjectId = projectId,
            FieldName = fieldName,
            Content = versionToRestore.Content,
            ChangeSummary = $"Restored from version {versionNumber}",
            ChangeType = "restore",
            DocumentId = versionToRestore.DocumentId,
            CreatedBy = restoredBy
        };
        
        return await CreateVersionAsync(restoredVersion);
    }
    
    public async Task<int> GetNextVersionNumberAsync(Guid projectId, string fieldName)
    {
        var maxVersion = await _context.DocumentVersions
            .Where(v => v.ProjectId == projectId && v.FieldName == fieldName)
            .Select(v => (int?)v.VersionNumber)
            .MaxAsync();
            
        return (maxVersion ?? 0) + 1;
    }
    
    public async Task<JsonDocument?> GetLatestContentAsync(Guid projectId, string fieldName)
    {
        var latestVersion = await _context.DocumentVersions
            .Where(v => v.ProjectId == projectId && v.FieldName == fieldName)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync();
            
        return latestVersion?.Content;
    }
}