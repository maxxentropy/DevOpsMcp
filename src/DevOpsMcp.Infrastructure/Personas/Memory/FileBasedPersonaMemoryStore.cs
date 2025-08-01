using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using DevOpsMcp.Application.Personas.Memory;
using DevOpsMcp.Domain.Personas;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevOpsMcp.Infrastructure.Personas.Memory;

public class FileBasedPersonaMemoryStore : IPersonaMemoryStore
{
    private readonly ILogger<FileBasedPersonaMemoryStore> _logger;
    private readonly PersonaMemoryStoreOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    public FileBasedPersonaMemoryStore(
        ILogger<FileBasedPersonaMemoryStore> logger,
        IOptions<PersonaMemoryStoreOptions> options)
    {
        _logger = logger;
        _options = options.Value;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        // Ensure base directory exists
        if (!Directory.Exists(_options.BasePath))
        {
            Directory.CreateDirectory(_options.BasePath);
            _logger.LogInformation("Created persona memory store directory: {Path}", _options.BasePath);
        }
    }

    public async Task<ConversationContext?> LoadContextAsync(string personaId, string sessionId)
    {
        var filePath = GetContextFilePath(personaId, sessionId);
        
        if (!File.Exists(filePath))
        {
            _logger.LogDebug("Context file not found: {Path}", filePath);
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var context = JsonSerializer.Deserialize<ConversationContext>(json, _jsonOptions);
            
            _logger.LogDebug("Loaded context from file: {Path}", filePath);
            return context;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading context from file: {Path}", filePath);
            return null;
        }
    }

    public async Task SaveContextAsync(string personaId, ConversationContext context)
    {
        var filePath = GetContextFilePath(personaId, context.SessionId);
        var directory = Path.GetDirectoryName(filePath);

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory!);
        }

        try
        {
            var json = JsonSerializer.Serialize(context, _jsonOptions);
            
            // Use async file write
            await File.WriteAllTextAsync(filePath, json);
            
            _logger.LogDebug("Saved context to file: {Path}", filePath);

            // Clean up old files if needed
            await CleanupOldFilesAsync(personaId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving context to file: {Path}", filePath);
            throw;
        }
    }

    public async Task DeleteContextAsync(string personaId, string sessionId)
    {
        var filePath = GetContextFilePath(personaId, sessionId);
        
        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
                _logger.LogDebug("Deleted context file: {Path}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting context file: {Path}", filePath);
            }
        }
        
        await Task.CompletedTask;
    }

    public async Task ClearPersonaDataAsync(string personaId)
    {
        var personaDirectory = GetPersonaDirectory(personaId);
        
        if (Directory.Exists(personaDirectory))
        {
            try
            {
                Directory.Delete(personaDirectory, recursive: true);
                _logger.LogInformation("Cleared all data for persona: {PersonaId}", personaId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing persona data: {PersonaId}", personaId);
            }
        }
        
        await Task.CompletedTask;
    }

    public async Task<List<string>> GetSessionIdsAsync(string personaId)
    {
        var personaDirectory = GetPersonaDirectory(personaId);
        var sessionIds = new List<string>();

        if (Directory.Exists(personaDirectory))
        {
            var contextFiles = Directory.GetFiles(personaDirectory, "context_*.json");
            
            foreach (var file in contextFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                if (fileName.StartsWith("context_", StringComparison.Ordinal))
                {
                    var sessionId = fileName.Substring("context_".Length);
                    sessionIds.Add(sessionId);
                }
            }
        }

        _logger.LogDebug("Found {Count} sessions for persona {PersonaId}", sessionIds.Count, personaId);
        return await Task.FromResult(sessionIds);
    }

    public async Task<LearningData?> GetLearningDataAsync(string personaId)
    {
        var filePath = GetLearningDataFilePath(personaId);
        
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var data = JsonSerializer.Deserialize<LearningData>(json, _jsonOptions);
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading learning data for persona: {PersonaId}", personaId);
            return null;
        }
    }

    public async Task<StorageMetrics> GetStorageMetricsAsync()
    {
        var metrics = new StorageMetrics();

        try
        {
            var allFiles = Directory.GetFiles(_options.BasePath, "*.json", SearchOption.AllDirectories);
            
            metrics.TotalEntries = allFiles.Length;
            metrics.TotalSize = allFiles.Sum(f => new FileInfo(f).Length);
            
            if (allFiles.Any())
            {
                metrics.OldestEntry = allFiles
                    .Select(f => new FileInfo(f).LastWriteTimeUtc)
                    .Min();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating storage metrics");
        }

        return await Task.FromResult(metrics);
    }

    private string GetPersonaDirectory(string personaId)
    {
        return Path.Combine(_options.BasePath, SanitizeFileName(personaId));
    }

    private string GetContextFilePath(string personaId, string sessionId)
    {
        return Path.Combine(
            GetPersonaDirectory(personaId),
            $"context_{SanitizeFileName(sessionId)}.json"
        );
    }

    private string GetLearningDataFilePath(string personaId)
    {
        return Path.Combine(
            GetPersonaDirectory(personaId),
            "learning_data.json"
        );
    }

    private string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return sanitized;
    }

    private async Task CleanupOldFilesAsync(string personaId)
    {
        if (_options.MaxContextsPerPersona <= 0)
            return;

        try
        {
            var personaDirectory = GetPersonaDirectory(personaId);
            if (!Directory.Exists(personaDirectory))
                return;

            var contextFiles = Directory.GetFiles(personaDirectory, "context_*.json")
                .Select(f => new FileInfo(f))
                .OrderByDescending(f => f.LastWriteTimeUtc)
                .ToList();

            if (contextFiles.Count > _options.MaxContextsPerPersona)
            {
                var filesToDelete = contextFiles.Skip(_options.MaxContextsPerPersona);
                
                foreach (var file in filesToDelete)
                {
                    try
                    {
                        file.Delete();
                        _logger.LogDebug("Deleted old context file: {Path}", file.FullName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error deleting old file: {Path}", file.FullName);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cleanup for persona: {PersonaId}", personaId);
        }

        await Task.CompletedTask;
    }

    public async Task SaveLearningDataAsync(string personaId, PersonaLearning learning)
    {
        var filePath = GetLearningDataFilePath(personaId);
        
        // Load existing data
        var learningData = await GetLearningDataAsync(personaId) ?? new LearningData();
        
        // Add new learning
        learningData.Learnings.Add(learning);
        learningData.LastUpdate = DateTime.UtcNow;
        learningData.PatternCount = learningData.Learnings.Count;
        learningData.AdaptationConfidence = learningData.Learnings.Average(l => l.ConfidenceScore);
        
        // Save updated data
        var json = JsonSerializer.Serialize(learningData, _jsonOptions);
        
        var directory = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory!);
        }
        await File.WriteAllTextAsync(filePath, json);
        
        _logger.LogDebug("Saved learning data for persona {PersonaId}", personaId);
    }

    public async Task<ProjectMemory?> LoadProjectMemoryAsync(string projectId)
    {
        var filePath = GetProjectMemoryFilePath(projectId);
        
        if (!File.Exists(filePath))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var memory = JsonSerializer.Deserialize<ProjectMemory>(json, _jsonOptions);
            return memory;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading project memory for {ProjectId}", projectId);
            return null;
        }
    }

    public async Task UpdateSharedKnowledgeAsync(string personaId, List<PersonaLearning> sharedLearnings)
    {
        var filePath = GetSharedKnowledgeFilePath(personaId);
        var directory = Path.GetDirectoryName(filePath);
        
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory!);
        }

        try
        {
            var json = JsonSerializer.Serialize(sharedLearnings, _jsonOptions);
            
            await File.WriteAllTextAsync(filePath, json);
            
            _logger.LogDebug("Updated shared knowledge for persona {PersonaId}", personaId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating shared knowledge for persona {PersonaId}", personaId);
        }
        
        await Task.CompletedTask;
    }

    public async Task CleanupOldDataAsync(DateTime cutoffDate)
    {
        _logger.LogInformation("Cleaning up data older than {CutoffDate}", cutoffDate);

        try
        {
            var allFiles = Directory.GetFiles(_options.BasePath, "*.json", SearchOption.AllDirectories);
            var filesToDelete = 0;

            foreach (var file in allFiles)
            {
                var fileInfo = new FileInfo(file);
                if (fileInfo.LastWriteTimeUtc < cutoffDate)
                {
                    try
                    {
                        File.Delete(file);
                        filesToDelete++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error deleting old file: {Path}", file);
                    }
                }
            }

            _logger.LogInformation("Deleted {Count} old files", filesToDelete);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during cleanup");
        }
        
        await Task.CompletedTask;
    }

    private string GetProjectMemoryFilePath(string projectId)
    {
        return Path.Combine(
            _options.BasePath,
            "projects",
            $"{SanitizeFileName(projectId)}_memory.json"
        );
    }

    private string GetSharedKnowledgeFilePath(string personaId)
    {
        return Path.Combine(
            GetPersonaDirectory(personaId),
            "shared_knowledge.json"
        );
    }
}

public class PersonaMemoryStoreOptions
{
    public string BasePath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DevOpsMcp", "PersonaMemory");
    public int MaxContextsPerPersona { get; set; } = 100;
    public int MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB
}