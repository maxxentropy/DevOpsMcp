namespace DevOpsMcp.Domain.Personas.Orchestration;

/// <summary>
/// Interface for orchestrating multiple personas and routing requests
/// </summary>
public interface IPersonaOrchestrator
{
    /// <summary>
    /// Selects the most appropriate persona for a given request
    /// </summary>
    Task<PersonaSelectionResult> SelectPersonaAsync(
        DevOpsContext context,
        string request,
        PersonaSelectionCriteria criteria);

    /// <summary>
    /// Coordinates multiple personas for complex tasks
    /// </summary>
    Task<OrchestrationResult> OrchestrateMultiPersonaResponseAsync(
        DevOpsContext context,
        string request,
        List<string> involvedPersonaIds);

    /// <summary>
    /// Routes a request to the selected persona
    /// </summary>
    Task<PersonaResponse> RouteRequestAsync(
        string personaId,
        DevOpsContext context,
        string request);

    /// <summary>
    /// Resolves conflicts between persona recommendations
    /// </summary>
    Task<ConflictResolution> ResolveConflictsAsync(
        List<PersonaResponse> responses,
        ConflictResolutionStrategy strategy);

    /// <summary>
    /// Gets current active personas
    /// </summary>
    Task<List<PersonaStatus>> GetActivePersonasAsync();

    /// <summary>
    /// Activates or deactivates a persona
    /// </summary>
    Task<bool> SetPersonaStatusAsync(string personaId, bool isActive);
}

/// <summary>
/// Criteria for selecting a persona
/// </summary>
public class PersonaSelectionCriteria
{
    public bool RequireSpecialization { get; set; }
    public List<DevOpsSpecialization> PreferredSpecializations { get; private set; } = new();
    public double MinimumConfidenceThreshold { get; set; } = 0.6;
    public bool AllowMultiplePersonas { get; set; }
    public int MaxPersonaCount { get; set; } = 3;
    public PersonaSelectionMode SelectionMode { get; set; } = PersonaSelectionMode.BestMatch;
}

/// <summary>
/// Modes for persona selection
/// </summary>
public enum PersonaSelectionMode
{
    BestMatch,
    RoundRobin,
    LoadBalanced,
    SpecializationBased,
    ContextAware
}

/// <summary>
/// Result of persona selection
/// </summary>
public class PersonaSelectionResult
{
    public string PrimaryPersonaId { get; set; } = string.Empty;
    public List<string> SecondaryPersonaIds { get; private set; } = new();
    public Dictionary<string, double> PersonaScores { get; private set; } = new();
    public string SelectionReason { get; set; } = string.Empty;
    public double Confidence { get; set; }
}

/// <summary>
/// Result of multi-persona orchestration
/// </summary>
public class OrchestrationResult
{
    public string OrchestrationId { get; set; } = Guid.NewGuid().ToString();
    public List<PersonaContribution> Contributions { get; private set; } = new();
    public string ConsolidatedResponse { get; set; } = string.Empty;
    public Dictionary<string, object> CombinedContext { get; private set; } = new();
    public OrchestrationMetrics Metrics { get; set; } = new();
}

/// <summary>
/// Contribution from a single persona
/// </summary>
public class PersonaContribution
{
    public string PersonaId { get; set; } = string.Empty;
    public PersonaResponse Response { get; set; } = new();
    public double Weight { get; set; }
    public ContributionType Type { get; set; }
}

/// <summary>
/// Types of contributions
/// </summary>
public enum ContributionType
{
    Primary,
    Supporting,
    Validation,
    Alternative
}

/// <summary>
/// Metrics for orchestration performance
/// </summary>
public class OrchestrationMetrics
{
    public double TotalDuration { get; set; }
    public Dictionary<string, double> PersonaDurations { get; private set; } = new();
    public int ConflictsResolved { get; set; }
    public double OverallConfidence { get; set; }
}

/// <summary>
/// Conflict resolution strategies
/// </summary>
public enum ConflictResolutionStrategy
{
    Consensus,
    HighestConfidence,
    SpecializationPriority,
    WeightedAverage,
    UserPreference
}

/// <summary>
/// Result of conflict resolution
/// </summary>
public class ConflictResolution
{
    public string ResolvedResponse { get; set; } = string.Empty;
    public Dictionary<string, string> ConflictingElements { get; private set; } = new();
    public string ResolutionMethod { get; set; } = string.Empty;
    public List<string> CompromisesMade { get; private set; } = new();
    public double Confidence { get; set; }
}

/// <summary>
/// Status of a persona
/// </summary>
public class PersonaStatus
{
    public string PersonaId { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime LastActivated { get; set; }
    public int CurrentLoad { get; set; }
    public double AverageResponseTime { get; set; }
    public PersonaHealth Health { get; set; } = new();
}

/// <summary>
/// Health metrics for a persona
/// </summary>
public class PersonaHealth
{
    public double SuccessRate { get; set; }
    public double AverageSatisfaction { get; set; }
    public int ErrorCount { get; set; }
    public DateTime LastError { get; set; }
    public HealthStatus Status { get; set; }
}

/// <summary>
/// Health status levels
/// </summary>
public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy,
    Unknown
}