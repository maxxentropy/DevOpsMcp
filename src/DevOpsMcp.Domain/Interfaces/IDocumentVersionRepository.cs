using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using DevOpsMcp.Domain.Entities.Enhanced;

namespace DevOpsMcp.Domain.Interfaces;

public interface IDocumentVersionRepository
{
    Task<DocumentVersion> CreateVersionAsync(DocumentVersion version);
    Task<List<DocumentVersion>> GetVersionHistoryAsync(Guid projectId, string fieldName);
    Task<DocumentVersion?> GetVersionAsync(Guid projectId, string fieldName, int versionNumber);
    Task<DocumentVersion> RestoreVersionAsync(Guid projectId, string fieldName, int versionNumber, string restoredBy);
    Task<int> GetNextVersionNumberAsync(Guid projectId, string fieldName);
    Task<JsonDocument?> GetLatestContentAsync(Guid projectId, string fieldName);
}