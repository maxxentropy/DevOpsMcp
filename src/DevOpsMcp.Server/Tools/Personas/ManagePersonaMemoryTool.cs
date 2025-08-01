using DevOpsMcp.Domain.Personas;
using DevOpsMcp.Server.Mcp;
using System.Text.Json;

namespace DevOpsMcp.Server.Tools.Personas;

/// <summary>
/// Tool for managing persona memory and conversation contexts
/// </summary>
public class ManagePersonaMemoryTool : BaseTool<ManagePersonaMemoryArguments>
{
    private readonly IPersonaMemoryManager _memoryManager;

    public ManagePersonaMemoryTool(IPersonaMemoryManager memoryManager)
    {
        _memoryManager = memoryManager;
    }

    public override string Name => "manage_persona_memory";
    
    public override string Description => 
        "Manage persona memory including retrieving memory snapshots, clearing memory, and getting memory statistics.";

    public override JsonElement InputSchema => CreateSchema<ManagePersonaMemoryArguments>();

    protected override async Task<CallToolResponse> ExecuteInternalAsync(
        ManagePersonaMemoryArguments arguments, 
        CancellationToken cancellationToken)
    {
        try
        {
            switch (arguments.Operation.ToLowerInvariant())
            {
                case "snapshot":
                    return await CreateSnapshotAsync(arguments.PersonaId!);
                    
                case "clear":
                    return await ClearMemoryAsync(arguments.PersonaId!, arguments.SessionId);
                    
                case "stats":
                    return await GetStatsAsync(arguments.PersonaId!);
                    
                case "sessions":
                    return await GetSessionsAsync(arguments.PersonaId!);
                    
                case "metrics":
                    return await GetMetricsAsync();
                    
                case "cleanup":
                    return await CleanupOldMemoriesAsync(arguments.RetentionDays ?? 30);
                    
                default:
                    return CreateErrorResponse($"Unknown operation: {arguments.Operation}");
            }
        }
        catch (Exception ex)
        {
            return CreateErrorResponse($"Error managing persona memory: {ex.Message}");
        }
    }

    private async Task<CallToolResponse> CreateSnapshotAsync(string personaId)
    {
        var snapshot = await _memoryManager.CreateMemorySnapshotAsync(personaId);
        return CreateJsonResponse(new
        {
            personaId = snapshot.PersonaId,
            snapshotTime = snapshot.SnapshotTime,
            totalConversations = snapshot.TotalConversations,
            totalInteractions = snapshot.TotalInteractions,
            averageInteractionsPerConversation = snapshot.AverageInteractionsPerConversation,
            successRate = snapshot.SuccessRate,
            commonTopics = snapshot.CommonTopics.OrderByDescending(kvp => kvp.Value).Take(10),
            recentConversations = snapshot.RecentContexts.Select(c => new
            {
                sessionId = c.SessionId,
                startTime = c.StartTime,
                lastInteraction = c.LastInteraction,
                interactionCount = c.InteractionCount,
                topics = c.Topics
            }),
            learningInsights = snapshot.LearningInsights
        });
    }

    private async Task<CallToolResponse> ClearMemoryAsync(string personaId, string? sessionId)
    {
        var success = await _memoryManager.ClearMemoryAsync(personaId, sessionId);
        
        if (success)
        {
            var message = string.IsNullOrEmpty(sessionId)
                ? $"Successfully cleared all memory for persona '{personaId}'"
                : $"Successfully cleared memory for session '{sessionId}' of persona '{personaId}'";
            return CreateSuccessResponse(message);
        }
        else
        {
            return CreateErrorResponse("Failed to clear memory");
        }
    }

    private async Task<CallToolResponse> GetStatsAsync(string personaId)
    {
        var stats = await _memoryManager.GetMemoryStatsAsync(personaId);
        return CreateJsonResponse(new
        {
            personaId = stats.PersonaId,
            totalConversations = stats.TotalConversations,
            totalInteractions = stats.TotalInteractions,
            memoryUsageBytes = stats.MemoryUsageBytes,
            memoryUsageMB = stats.MemoryUsageBytes / (1024.0 * 1024.0),
            oldestMemory = stats.OldestMemory,
            newestMemory = stats.NewestMemory,
            averageSessionLength = stats.AverageSessionLength,
            projectCounts = stats.ProjectCounts
        });
    }

    private async Task<CallToolResponse> GetSessionsAsync(string personaId)
    {
        var sessions = await _memoryManager.GetActiveSessionsAsync(personaId);
        return CreateJsonResponse(new
        {
            personaId,
            totalSessions = sessions.Count,
            sessionIds = sessions
        });
    }

    private async Task<CallToolResponse> GetMetricsAsync()
    {
        var metrics = await _memoryManager.GetMemoryMetricsAsync();
        return CreateJsonResponse(new
        {
            timestamp = metrics.Timestamp,
            activeContextCount = metrics.ActiveContextCount,
            totalMemoryUsageBytes = metrics.TotalMemoryUsage,
            totalMemoryUsageMB = metrics.TotalMemoryUsage / (1024.0 * 1024.0),
            contextsByPersona = metrics.ContextsByPersona,
            cachePerformance = new
            {
                hitRate = metrics.CacheHitRate,
                missRate = metrics.CacheMissRate
            },
            persistentStorageSizeBytes = metrics.PersistentStorageSize,
            persistentStorageSizeMB = metrics.PersistentStorageSize / (1024.0 * 1024.0),
            oldestContext = metrics.OldestContext
        });
    }

    private async Task<CallToolResponse> CleanupOldMemoriesAsync(int retentionDays)
    {
        var retention = TimeSpan.FromDays(retentionDays);
        await _memoryManager.CleanupOldMemoriesAsync(retention);
        return CreateSuccessResponse($"Successfully cleaned up memories older than {retentionDays} days");
    }
}

public class ManagePersonaMemoryArguments
{
    /// <summary>
    /// Operation to perform: 'snapshot', 'clear', 'stats', 'sessions', 'metrics', 'cleanup'
    /// </summary>
    public string Operation { get; set; } = string.Empty;
    
    /// <summary>
    /// Persona ID (required for snapshot, clear, stats, sessions operations)
    /// </summary>
    public string? PersonaId { get; set; }
    
    /// <summary>
    /// Session ID (optional, only for clear operation to clear specific session)
    /// </summary>
    public string? SessionId { get; set; }
    
    /// <summary>
    /// Retention period in days (only for cleanup operation, default: 30)
    /// </summary>
    public int? RetentionDays { get; set; }
}