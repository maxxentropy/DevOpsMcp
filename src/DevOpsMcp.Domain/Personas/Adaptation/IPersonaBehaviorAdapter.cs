namespace DevOpsMcp.Domain.Personas.Adaptation;

/// <summary>
/// Interface for adapting persona behavior based on context and user interactions
/// </summary>
public interface IPersonaBehaviorAdapter
{
    /// <summary>
    /// Analyzes user interaction patterns and returns behavior adjustments
    /// </summary>
    Task<BehaviorAdjustment> AnalyzeInteractionPatternAsync(
        string personaId, 
        UserInteraction interaction,
        InteractionHistory history);

    /// <summary>
    /// Adapts persona configuration based on learned preferences
    /// </summary>
    Task<PersonaConfiguration> AdaptConfigurationAsync(
        string personaId,
        PersonaConfiguration currentConfig,
        UserPreferences preferences,
        ProjectContext context);

    /// <summary>
    /// Calculates confidence score for behavior adaptation
    /// </summary>
    Task<double> CalculateAdaptationConfidenceAsync(
        string personaId,
        InteractionHistory history);

    /// <summary>
    /// Learns from feedback and updates adaptation model
    /// </summary>
    Task LearnFromFeedbackAsync(
        string personaId,
        UserFeedback feedback,
        PersonaResponse response);
}

/// <summary>
/// Represents a user interaction with the system
/// </summary>
public class UserInteraction
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Request { get; set; } = string.Empty;
    public string PersonaId { get; set; } = string.Empty;
    public Dictionary<string, object> Context { get; private set; } = new();
    public InteractionType Type { get; set; }
    public double Duration { get; set; }
}

/// <summary>
/// Types of user interactions
/// </summary>
public enum InteractionType
{
    Query,
    Command,
    Feedback,
    Clarification,
    FollowUp
}

/// <summary>
/// Represents behavior adjustments to apply
/// </summary>
public class BehaviorAdjustment
{
    public Dictionary<string, double> ParameterAdjustments { get; private set; } = new();
    public CommunicationStyle? SuggestedCommunicationStyle { get; set; }
    public TechnicalDepth? SuggestedTechnicalDepth { get; set; }
    public double ConfidenceScore { get; set; }
    public List<string> Reasons { get; private set; } = new();
}

/// <summary>
/// User preferences learned over time
/// </summary>
public class UserPreferences
{
    public string UserId { get; set; } = string.Empty;
    public PreferredCommunicationStyle CommunicationPreference { get; set; }
    public TechnicalDepth PreferredTechnicalDepth { get; set; }
    public List<string> PreferredTools { get; private set; } = new();
    public List<string> AvoidedTopics { get; private set; } = new();
    public Dictionary<string, double> TopicInterestScores { get; private set; } = new();
    public ResponseLength PreferredResponseLength { get; set; }
    public bool PrefersExamples { get; set; }
    public bool PrefersVisualAids { get; set; }
    public DateTime LastUpdated { get; set; }
}

/// <summary>
/// Preferred communication styles
/// </summary>
public enum PreferredCommunicationStyle
{
    Concise,
    Standard,
    Detailed,
    StepByStep,
    Conceptual,
    Practical
}

/// <summary>
/// Preferred response length
/// </summary>
public enum ResponseLength
{
    Brief,
    Standard,
    Comprehensive
}

/// <summary>
/// User feedback on persona responses
/// </summary>
public class UserFeedback
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ResponseId { get; set; } = string.Empty;
    public FeedbackType Type { get; set; }
    public double Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public List<string> ImprovementSuggestions { get; private set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Types of feedback
/// </summary>
public enum FeedbackType
{
    Positive,
    Negative,
    Neutral,
    Clarification,
    Correction
}

/// <summary>
/// History of user interactions
/// </summary>
public class InteractionHistory
{
    public string UserId { get; set; } = string.Empty;
    public List<UserInteraction> RecentInteractions { get; private set; } = new();
    public Dictionary<string, int> PersonaUsageCount { get; private set; } = new();
    public Dictionary<string, double> PersonaSatisfactionScores { get; private set; } = new();
    public List<UserFeedback> Feedback { get; private set; } = new();
    public DateTime FirstInteraction { get; set; }
    public DateTime LastInteraction { get; set; }
    public int TotalInteractions { get; set; }
}