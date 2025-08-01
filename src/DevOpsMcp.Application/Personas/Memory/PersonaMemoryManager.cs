using DevOpsMcp.Domain.Personas;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DevOpsMcp.Application.Personas.Memory;

public class PersonaMemoryManager : IPersonaMemoryManager
{
    private readonly ILogger<PersonaMemoryManager> _logger;
    private readonly IDistributedCache _cache;
    private readonly IPersonaMemoryStore _persistentStore;
    private readonly Dictionary<string, ConversationContext> _activeContexts;
    private readonly object _lockObject = new();
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(24);

    public PersonaMemoryManager(
        ILogger<PersonaMemoryManager> logger,
        IDistributedCache cache,
        IPersonaMemoryStore persistentStore)
    {
        _logger = logger;
        _cache = cache;
        _persistentStore = persistentStore;
        _activeContexts = new Dictionary<string, ConversationContext>();
    }

    public async Task<ConversationContext?> RetrieveConversationContextAsync(string personaId, string sessionId)
    {
        var key = GetContextKey(personaId, sessionId);
        
        // Check in-memory cache first
        lock (_lockObject)
        {
            if (_activeContexts.TryGetValue(key, out var activeContext))
            {
                _logger.LogDebug("Retrieved context from active memory for {PersonaId}/{SessionId}", personaId, sessionId);
                return activeContext;
            }
        }

        // Check distributed cache
        try
        {
            var cachedData = await _cache.GetStringAsync(key);
            if (!string.IsNullOrEmpty(cachedData))
            {
                var context = JsonSerializer.Deserialize<ConversationContext>(cachedData);
                if (context != null)
                {
                    _logger.LogDebug("Retrieved context from distributed cache for {PersonaId}/{SessionId}", personaId, sessionId);
                    
                    // Store in active memory
                    lock (_lockObject)
                    {
                        _activeContexts[key] = context;
                    }
                    
                    return context;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving from distributed cache");
        }

        // Check persistent store
        var persistedContext = await _persistentStore.LoadContextAsync(personaId, sessionId);
        if (persistedContext != null)
        {
            _logger.LogDebug("Retrieved context from persistent store for {PersonaId}/{SessionId}", personaId, sessionId);
            
            // Cache for future use
            await CacheContextAsync(key, persistedContext);
            
            // Store in active memory
            lock (_lockObject)
            {
                _activeContexts[key] = persistedContext;
            }
            
            return persistedContext;
        }

        _logger.LogDebug("No existing context found for {PersonaId}/{SessionId}", personaId, sessionId);
        return null;
    }

    public async Task StoreConversationContextAsync(string personaId, ConversationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var key = GetContextKey(personaId, context.SessionId);
        
        _logger.LogDebug("Storing context for {PersonaId}/{SessionId}", personaId, context.SessionId);

        // Store in active memory
        lock (_lockObject)
        {
            _activeContexts[key] = context;
        }

        // Store in distributed cache
        await CacheContextAsync(key, context);

        // Persist to storage (async, don't wait)
        _ = Task.Run(async () =>
        {
            try
            {
                await _persistentStore.SaveContextAsync(personaId, context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error persisting context to storage");
            }
        });
    }

    public async Task<PersonaMemorySnapshot> CreateMemorySnapshotAsync(string personaId)
    {
        _logger.LogInformation("Creating memory snapshot for persona {PersonaId}", personaId);

        var snapshot = new PersonaMemorySnapshot
        {
            PersonaId = personaId,
            SnapshotTime = DateTime.UtcNow
        };

        // Get all active contexts for this persona
        List<ConversationContext> personaContexts;
        lock (_lockObject)
        {
            personaContexts = _activeContexts
                .Where(kvp => kvp.Key.StartsWith($"{personaId}:", StringComparison.Ordinal))
                .Select(kvp => kvp.Value)
                .ToList();
        }

        // Calculate statistics
        if (personaContexts.Any())
        {
            snapshot.TotalConversations = personaContexts.Count;
            snapshot.TotalInteractions = personaContexts.Sum(c => c.InteractionHistory.Count);
            snapshot.AverageInteractionsPerConversation = 
                (double)snapshot.TotalInteractions / snapshot.TotalConversations;

            // Extract common patterns
            var allTopics = personaContexts
                .SelectMany(c => c.InteractionHistory)
                .SelectMany(i => i.TopicsDiscussed)
                .GroupBy(t => t)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var topic in allTopics)
                snapshot.CommonTopics[topic.Key] = topic.Value;

            // Success metrics
            var successfulInteractions = personaContexts
                .SelectMany(c => c.InteractionHistory)
                .Count(i => i.WasSuccessful);
            
            snapshot.SuccessRate = personaContexts.Sum(c => c.InteractionHistory.Count) > 0
                ? (double)successfulInteractions / personaContexts.Sum(c => c.InteractionHistory.Count)
                : 0;

            // Get recent contexts
            snapshot.RecentContexts.AddRange(
                personaContexts
                    .OrderByDescending(c => c.LastInteraction)
                    .Take(5)
                    .Select(c => 
                    {
                        var summary = new ConversationSummary
                        {
                            SessionId = c.SessionId,
                            StartTime = c.InteractionHistory.FirstOrDefault()?.Timestamp ?? c.LastInteraction,
                            LastInteraction = c.LastInteraction,
                            InteractionCount = c.InteractionHistory.Count
                        };
                        var topics = c.InteractionHistory.SelectMany(i => i.TopicsDiscussed).Distinct();
                        foreach (var topic in topics)
                            summary.Topics.Add(topic);
                        return summary;
                    })
            );
        }

        // Get learning insights from persistent store
        var learningData = await _persistentStore.GetLearningDataAsync(personaId);
        if (learningData != null)
        {
            snapshot.LearningInsights["total_learned_patterns"] = learningData.PatternCount;
            snapshot.LearningInsights["adaptation_confidence"] = learningData.AdaptationConfidence;
            snapshot.LearningInsights["last_learning_update"] = learningData.LastUpdate.ToString("O");
        }

        return snapshot;
    }

    public async Task<bool> ClearMemoryAsync(string personaId, string? sessionId = null)
    {
        _logger.LogWarning("Clearing memory for persona {PersonaId}, session: {SessionId}", 
            personaId, sessionId ?? "ALL");

        try
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                // Clear all sessions for persona
                List<string> keysToRemove;
                lock (_lockObject)
                {
                    keysToRemove = _activeContexts.Keys
                        .Where(k => k.StartsWith($"{personaId}:", StringComparison.Ordinal))
                        .ToList();
                    
                    foreach (var key in keysToRemove)
                    {
                        _activeContexts.Remove(key);
                    }
                }

                // Clear from cache
                foreach (var key in keysToRemove)
                {
                    await _cache.RemoveAsync(key);
                }

                // Clear from persistent store
                await _persistentStore.ClearPersonaDataAsync(personaId);
            }
            else
            {
                // Clear specific session
                var key = GetContextKey(personaId, sessionId);
                
                lock (_lockObject)
                {
                    _activeContexts.Remove(key);
                }

                await _cache.RemoveAsync(key);
                await _persistentStore.DeleteContextAsync(personaId, sessionId);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing memory");
            return false;
        }
    }

    public async Task<List<string>> GetActiveSessionsAsync(string personaId)
    {
        List<string> activeSessions;
        
        lock (_lockObject)
        {
            activeSessions = _activeContexts.Keys
                .Where(k => k.StartsWith($"{personaId}:", StringComparison.Ordinal))
                .Select(k => k.Substring(personaId.Length + 1))
                .ToList();
        }

        // Also check persistent store for additional sessions
        var persistedSessions = await _persistentStore.GetSessionIdsAsync(personaId);
        
        // Merge and deduplicate
        var allSessions = activeSessions.Union(persistedSessions).Distinct().ToList();
        
        _logger.LogDebug("Found {Count} active sessions for persona {PersonaId}", 
            allSessions.Count, personaId);
        
        return allSessions;
    }

    public async Task<MemoryMetrics> GetMemoryMetricsAsync()
    {
        var metrics = new MemoryMetrics
        {
            Timestamp = DateTime.UtcNow
        };

        lock (_lockObject)
        {
            metrics.ActiveContextCount = _activeContexts.Count;
            metrics.TotalMemoryUsage = EstimateMemoryUsage(_activeContexts.Values);
            
            var contextsByPersona = _activeContexts.Keys
                .Select(k => k.Split(':')[0])
                .GroupBy(p => p)
                .ToDictionary(g => g.Key, g => g.Count());
            foreach (var persona in contextsByPersona)
                metrics.ContextsByPersona[persona.Key] = persona.Value;
        }

        // Get cache statistics if available
        if (_cache is IExtendedDistributedCache extendedCache)
        {
            var cacheStats = await extendedCache.GetStatisticsAsync();
            metrics.CacheHitRate = cacheStats.HitRate;
            metrics.CacheMissRate = cacheStats.MissRate;
        }

        // Get storage metrics
        var storageMetrics = await _persistentStore.GetStorageMetricsAsync();
        metrics.PersistentStorageSize = storageMetrics.TotalSize;
        metrics.OldestContext = storageMetrics.OldestEntry;
        
        return metrics;
    }

    private string GetContextKey(string personaId, string sessionId)
    {
        return $"{personaId}:{sessionId}";
    }

    private async Task CacheContextAsync(string key, ConversationContext context)
    {
        try
        {
            var json = JsonSerializer.Serialize(context);
            var options = new DistributedCacheEntryOptions
            {
                SlidingExpiration = _cacheExpiration
            };
            
            await _cache.SetStringAsync(key, json, options);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error caching context");
        }
    }

    private long EstimateMemoryUsage(IEnumerable<ConversationContext> contexts)
    {
        // Rough estimation based on typical object sizes
        long totalBytes = 0;
        
        foreach (var context in contexts)
        {
            // Base object overhead
            totalBytes += 24;
            
            // String fields (estimate 2 bytes per char)
            totalBytes += (context.SessionId.Length + context.PersonaId.Length) * 2;
            
            // Interaction history
            totalBytes += context.InteractionHistory.Count * 200; // Rough estimate per interaction
            
            // Metrics
            totalBytes += 32; // Various numeric fields
            
            // Current state
            if (context.CurrentProjectState != null)
            {
                totalBytes += 100; // Rough estimate
            }
        }
        
        return totalBytes;
    }

    public async Task UpdatePersonaLearningAsync(string personaId, PersonaLearning learning)
    {
        _logger.LogDebug("Updating learning for persona {PersonaId}", personaId);
        
        // Store learning data in persistent store
        await _persistentStore.SaveLearningDataAsync(personaId, learning);
    }

    public async Task<ProjectMemory?> GetProjectMemoryAsync(string projectId)
    {
        _logger.LogDebug("Retrieving project memory for {ProjectId}", projectId);
        
        // Aggregate memories from all personas for this project
        var projectMemory = new ProjectMemory
        {
            ProjectId = projectId,
            CreatedAt = DateTime.UtcNow,
            LastUpdated = DateTime.UtcNow
        };

        // Get all active contexts that mention this project
        lock (_lockObject)
        {
            var projectContexts = _activeContexts.Values
                .Where(c => c.CurrentProjectState.ProjectId == projectId)
                .ToList();

            foreach (var context in projectContexts)
            {
                if (!projectMemory.PersonaConversations.TryGetValue(context.PersonaId, out var conversations))
                {
                    conversations = new List<ConversationContext>();
                    projectMemory.PersonaConversations[context.PersonaId] = conversations;
                }
                conversations.Add(context);
            }
        }

        // Load additional data from persistent store
        var persistedMemory = await _persistentStore.LoadProjectMemoryAsync(projectId);
        if (persistedMemory != null)
        {
            // Merge with in-memory data
            foreach (var kvp in persistedMemory.PersonaConversations)
            {
                if (!projectMemory.PersonaConversations.TryGetValue(kvp.Key, out var _))
                {
                    projectMemory.PersonaConversations[kvp.Key] = kvp.Value;
                }
            }
            
            projectMemory.SignificantEvents.AddRange(persistedMemory.SignificantEvents);
            projectMemory.LessonsLearned.AddRange(persistedMemory.LessonsLearned);
            
            foreach (var kvp in persistedMemory.ProjectKnowledge)
            {
                projectMemory.ProjectKnowledge[kvp.Key] = kvp.Value;
            }
        }

        return projectMemory;
    }

    public async Task SyncCrossPersonaKnowledgeAsync(IEnumerable<string> personaIds)
    {
        var personaIdsList = personaIds.ToList();
        _logger.LogInformation("Syncing knowledge across {Count} personas", personaIdsList.Count);
        
        var sharedKnowledge = new Dictionary<string, object>();
        var allLearnings = new List<PersonaLearning>();

        // Collect knowledge from all personas
        foreach (var personaId in personaIdsList)
        {
            var learningData = await _persistentStore.GetLearningDataAsync(personaId);
            if (learningData != null && learningData.Learnings != null)
            {
                allLearnings.AddRange(learningData.Learnings);
            }
        }

        // Identify common patterns and insights
        var commonPatterns = allLearnings
            .GroupBy(l => l.Subject)
            .Where(g => g.Count() > 1)
            .Select(g => new
            {
                Subject = g.Key,
                Count = g.Count(),
                AverageConfidence = g.Average(l => l.ConfidenceScore)
            })
            .ToList();

        // Share verified learnings across personas
        foreach (var personaId in personaIdsList)
        {
            var relevantLearnings = allLearnings
                .Where(l => l.PersonaId != personaId && l.IsVerified && l.ConfidenceScore > 0.8)
                .ToList();

            if (relevantLearnings.Any())
            {
                await _persistentStore.UpdateSharedKnowledgeAsync(personaId, relevantLearnings);
            }
        }
    }

    public async Task<PersonaMemoryStats> GetMemoryStatsAsync(string personaId)
    {
        _logger.LogDebug("Getting memory stats for persona {PersonaId}", personaId);
        
        var stats = new PersonaMemoryStats
        {
            PersonaId = personaId
        };

        // Count active contexts
        lock (_lockObject)
        {
            var personaContexts = _activeContexts
                .Where(kvp => kvp.Key.StartsWith($"{personaId}:", StringComparison.Ordinal))
                .Select(kvp => kvp.Value)
                .ToList();

            stats.TotalConversations = personaContexts.Count;
            stats.TotalInteractions = personaContexts.Sum(c => c.InteractionHistory.Count);
            stats.MemoryUsageBytes = EstimateMemoryUsage(personaContexts);
            
            if (personaContexts.Any())
            {
                stats.OldestMemory = personaContexts.Min(c => c.StartTime);
                stats.NewestMemory = personaContexts.Max(c => c.LastInteraction);
                stats.AverageSessionLength = personaContexts.Average(c => c.InteractionHistory.Count);
                
                // Count by project
                var projectGroups = personaContexts
                    .GroupBy(c => c.CurrentProjectState.ProjectId)
                    .Where(g => !string.IsNullOrEmpty(g.Key));
                
                foreach (var group in projectGroups)
                {
                    stats.ProjectCounts[group.Key] = group.Count();
                }
            }
        }

        // Get additional stats from persistent store
        var storageMetrics = await _persistentStore.GetStorageMetricsAsync();
        if (storageMetrics.OldestEntry < stats.OldestMemory)
        {
            stats.OldestMemory = storageMetrics.OldestEntry;
        }

        return stats;
    }

    public async Task CleanupOldMemoriesAsync(TimeSpan retention)
    {
        _logger.LogInformation("Cleaning up memories older than {Retention}", retention);
        
        var cutoffDate = DateTime.UtcNow - retention;
        
        // Clean up active contexts
        List<string> keysToRemove;
        lock (_lockObject)
        {
            keysToRemove = _activeContexts
                .Where(kvp => kvp.Value.LastInteraction < cutoffDate)
                .Select(kvp => kvp.Key)
                .ToList();
            
            foreach (var key in keysToRemove)
            {
                _activeContexts.Remove(key);
            }
        }

        // Clean up cache
        foreach (var key in keysToRemove)
        {
            await _cache.RemoveAsync(key);
        }

        // Clean up persistent store
        await _persistentStore.CleanupOldDataAsync(cutoffDate);
        
        _logger.LogInformation("Cleaned up {Count} old memory entries", keysToRemove.Count);
    }
}

/// <summary>
/// Interface for extended cache functionality
/// </summary>
public interface IExtendedDistributedCache : IDistributedCache
{
    Task<CacheStatistics> GetStatisticsAsync();
}

/// <summary>
/// Cache statistics
/// </summary>
public class CacheStatistics
{
    public double HitRate { get; set; }
    public double MissRate { get; set; }
    public long TotalRequests { get; set; }
    public long CacheHits { get; set; }
    public long CacheMisses { get; set; }
}

/// <summary>
/// Interface for persistent memory storage
/// </summary>
public interface IPersonaMemoryStore
{
    Task<ConversationContext?> LoadContextAsync(string personaId, string sessionId);
    Task SaveContextAsync(string personaId, ConversationContext context);
    Task DeleteContextAsync(string personaId, string sessionId);
    Task ClearPersonaDataAsync(string personaId);
    Task<List<string>> GetSessionIdsAsync(string personaId);
    Task<LearningData?> GetLearningDataAsync(string personaId);
    Task<StorageMetrics> GetStorageMetricsAsync();
    Task SaveLearningDataAsync(string personaId, PersonaLearning learning);
    Task<ProjectMemory?> LoadProjectMemoryAsync(string projectId);
    Task UpdateSharedKnowledgeAsync(string personaId, List<PersonaLearning> sharedLearnings);
    Task CleanupOldDataAsync(DateTime cutoffDate);
}

/// <summary>
/// Learning data stored for a persona
/// </summary>
public class LearningData
{
    public int PatternCount { get; set; }
    public double AdaptationConfidence { get; set; }
    public DateTime LastUpdate { get; set; }
    public List<PersonaLearning> Learnings { get; private set; } = new();
}

/// <summary>
/// Storage metrics
/// </summary>
public class StorageMetrics
{
    public long TotalSize { get; set; }
    public DateTime OldestEntry { get; set; }
    public int TotalEntries { get; set; }
}