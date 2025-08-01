namespace DevOpsMcp.Domain.Personas;

public interface IPersonaMemoryManager
{
    Task StoreConversationContextAsync(string personaId, ConversationContext context);
    Task<ConversationContext?> RetrieveConversationContextAsync(string personaId, string sessionId);
    Task UpdatePersonaLearningAsync(string personaId, PersonaLearning learning);
    Task<ProjectMemory?> GetProjectMemoryAsync(string projectId);
    Task SyncCrossPersonaKnowledgeAsync(IEnumerable<string> personaIds);
    Task<PersonaMemoryStats> GetMemoryStatsAsync(string personaId);
    Task CleanupOldMemoriesAsync(TimeSpan retention);
    Task<PersonaMemorySnapshot> CreateMemorySnapshotAsync(string personaId);
    Task<bool> ClearMemoryAsync(string personaId, string? sessionId = null);
    Task<List<string>> GetActiveSessionsAsync(string personaId);
    Task<MemoryMetrics> GetMemoryMetricsAsync();
}

public class ConversationContext
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString();
    public string PersonaId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime LastInteraction { get; set; } = DateTime.UtcNow;
    public List<InteractionSummary> InteractionHistory { get; private set; } = new();
    public Dictionary<string, object> UserPreferences { get; private set; } = new();
    public ProjectState CurrentProjectState { get; set; } = new();
    public List<PendingTask> PendingTasks { get; private set; } = new();
    public Dictionary<string, object> LearningsAndInsights { get; private set; } = new();
    public ConversationMetrics Metrics { get; set; } = new();
}

public class InteractionSummary
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string UserInput { get; set; } = string.Empty;
    public string PersonaResponse { get; set; } = string.Empty;
    public string Intent { get; set; } = string.Empty;
    public Dictionary<string, object> ExtractedEntities { get; private set; } = new();
    public List<string> TopicsDiscussed { get; private set; } = new();
    public double SentimentScore { get; set; }
    public bool WasSuccessful { get; set; } = true;
}

public class ProjectState
{
    public string ProjectId { get; set; } = string.Empty;
    public string CurrentPhase { get; set; } = string.Empty;
    public Dictionary<string, object> StateVariables { get; private set; } = new();
    public List<CompletedTask> CompletedTasks { get; private set; } = new();
    public List<string> ActiveIssues { get; private set; } = new();
    public Dictionary<string, double> Metrics { get; private set; } = new();
}

public class CompletedTask
{
    public string TaskId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; }
    public string CompletedBy { get; set; } = string.Empty;
    public Dictionary<string, object> Results { get; private set; } = new();
}

public class PendingTask
{
    public string TaskId { get; set; } = Guid.NewGuid().ToString();
    public string Description { get; set; } = string.Empty;
    public TaskPriority Priority { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }
    public string AssignedPersona { get; set; } = string.Empty;
    public Dictionary<string, object> Context { get; private set; } = new();
    public List<string> Dependencies { get; private set; } = new();
}

public enum TaskPriority
{
    Low,
    Medium,
    High,
    Critical
}

public class PersonaLearning
{
    public string PersonaId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public LearningType Type { get; set; }
    public string Subject { get; set; } = string.Empty;
    public Dictionary<string, object> LearnedData { get; private set; } = new();
    public double ConfidenceScore { get; set; }
    public string Source { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
}

public enum LearningType
{
    UserPreference,
    ProjectPattern,
    BestPractice,
    ErrorCorrection,
    PerformanceOptimization,
    SecurityInsight
}

public class ProjectMemory
{
    public string ProjectId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdated { get; set; }
    public Dictionary<string, List<ConversationContext>> PersonaConversations { get; private set; } = new();
    public List<ProjectEvent> SignificantEvents { get; private set; } = new();
    public Dictionary<string, object> ProjectKnowledge { get; private set; } = new();
    public List<LessonLearned> LessonsLearned { get; private set; } = new();
    public ProjectMetrics Metrics { get; set; } = new();
}

public class ProjectEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Impact { get; set; } = string.Empty;
    public Dictionary<string, object> Details { get; private set; } = new();
}

public class LessonLearned
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<string> Tags { get; private set; } = new();
    public string RecommendedAction { get; set; } = string.Empty;
    public double ImpactScore { get; set; }
}

public class ProjectMetrics
{
    public int TotalInteractions { get; set; }
    public int TotalTasks { get; set; }
    public double SuccessRate { get; set; }
    public Dictionary<string, int> PersonaUsage { get; private set; } = new();
    public Dictionary<string, double> CategoryPerformance { get; private set; } = new();
    public TimeSpan AverageResponseTime { get; set; }
}

public class ConversationMetrics
{
    public int TotalExchanges { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public double AverageResponseTime { get; set; }
    public double UserSatisfactionScore { get; set; }
    public Dictionary<string, int> TopicFrequency { get; private set; } = new();
    public List<string> ResolvedIssues { get; private set; } = new();
}

public class PersonaMemoryStats
{
    public string PersonaId { get; set; } = string.Empty;
    public int TotalConversations { get; set; }
    public int TotalInteractions { get; set; }
    public long MemoryUsageBytes { get; set; }
    public DateTime OldestMemory { get; set; }
    public DateTime NewestMemory { get; set; }
    public Dictionary<string, int> ProjectCounts { get; private set; } = new();
    public double AverageSessionLength { get; set; }
}

public class PersonaMemorySnapshot
{
    public string PersonaId { get; set; } = string.Empty;
    public DateTime SnapshotTime { get; set; }
    public int TotalConversations { get; set; }
    public int TotalInteractions { get; set; }
    public double AverageInteractionsPerConversation { get; set; }
    public Dictionary<string, int> CommonTopics { get; private set; } = new();
    public double SuccessRate { get; set; }
    public List<ConversationSummary> RecentContexts { get; private set; } = new();
    public Dictionary<string, object> LearningInsights { get; private set; } = new();
}

public class ConversationSummary
{
    public string SessionId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime LastInteraction { get; set; }
    public int InteractionCount { get; set; }
    public List<string> Topics { get; private set; } = new();
}

public class MemoryMetrics
{
    public DateTime Timestamp { get; set; }
    public int ActiveContextCount { get; set; }
    public long TotalMemoryUsage { get; set; }
    public Dictionary<string, int> ContextsByPersona { get; private set; } = new();
    public double CacheHitRate { get; set; }
    public double CacheMissRate { get; set; }
    public long PersistentStorageSize { get; set; }
    public DateTime OldestContext { get; set; }
}