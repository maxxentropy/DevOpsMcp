namespace DevOpsMcp.Domain.Personas.Adaptation;

/// <summary>
/// Interface for the persona learning engine that tracks and learns from interactions
/// </summary>
public interface IPersonaLearningEngine
{
    /// <summary>
    /// Processes an interaction and extracts learning insights
    /// </summary>
    Task<LearningInsights> ProcessInteractionAsync(
        UserInteraction interaction,
        PersonaResponse response);

    /// <summary>
    /// Updates user preference model based on interactions
    /// </summary>
    Task<UserPreferences> UpdateUserPreferencesAsync(
        string userId,
        LearningInsights insights,
        UserPreferences currentPreferences);

    /// <summary>
    /// Predicts user preferences for new users based on similar profiles
    /// </summary>
    Task<UserPreferences> PredictInitialPreferencesAsync(
        UserProfile userProfile,
        ProjectContext context);

    /// <summary>
    /// Analyzes patterns across all users to improve persona behaviors
    /// </summary>
    Task<GlobalLearningPatterns> AnalyzeGlobalPatternsAsync();

    /// <summary>
    /// Exports learning model for backup or analysis
    /// </summary>
    Task<LearningModel> ExportModelAsync(string personaId);

    /// <summary>
    /// Imports a learning model
    /// </summary>
    Task ImportModelAsync(string personaId, LearningModel model);
}

/// <summary>
/// Insights extracted from user interactions
/// </summary>
public class LearningInsights
{
    public string InteractionId { get; set; } = string.Empty;
    public Dictionary<string, double> PreferenceSignals { get; private set; } = new();
    public List<string> PositiveIndicators { get; private set; } = new();
    public List<string> NegativeIndicators { get; private set; } = new();
    public CommunicationEffectiveness CommunicationScore { get; set; } = new();
    public TechnicalAlignmentScore TechnicalAlignment { get; set; } = new();
    public double OverallEffectiveness { get; set; }
}

/// <summary>
/// Measures how effective the communication was
/// </summary>
public class CommunicationEffectiveness
{
    public double Clarity { get; set; }
    public double Relevance { get; set; }
    public double Completeness { get; set; }
    public double Appropriateness { get; set; }
    public double Overall => (Clarity + Relevance + Completeness + Appropriateness) / 4.0;
}

/// <summary>
/// Measures technical alignment with user's level
/// </summary>
public class TechnicalAlignmentScore
{
    public bool TooSimple { get; set; }
    public bool TooComplex { get; set; }
    public bool JustRight { get; set; }
    public double AlignmentScore { get; set; }
    public List<string> MisalignedConcepts { get; private set; } = new();
}

/// <summary>
/// Global patterns learned across all users
/// </summary>
public class GlobalLearningPatterns
{
    public Dictionary<string, PersonaEffectiveness> PersonaEffectiveness { get; private set; } = new();
    public Dictionary<string, double> TopicPopularity { get; private set; } = new();
    public Dictionary<string, List<string>> CommonWorkflows { get; private set; } = new();
    public List<CommonMisunderstanding> FrequentMisunderstandings { get; private set; } = new();
    public Dictionary<string, PreferenceCluster> UserClusters { get; private set; } = new();
}

/// <summary>
/// Effectiveness metrics for a persona
/// </summary>
public class PersonaEffectiveness
{
    public string PersonaId { get; set; } = string.Empty;
    public double OverallSatisfaction { get; set; }
    public Dictionary<TaskCategory, double> CategoryEffectiveness { get; private set; } = new();
    public Dictionary<string, double> StrengthAreas { get; private set; } = new();
    public Dictionary<string, double> ImprovementAreas { get; private set; } = new();
    public int TotalInteractions { get; set; }
}

/// <summary>
/// Common misunderstandings identified
/// </summary>
public class CommonMisunderstanding
{
    public string Pattern { get; set; } = string.Empty;
    public List<string> Examples { get; private set; } = new();
    public string RecommendedClarification { get; set; } = string.Empty;
    public double Frequency { get; set; }
}

/// <summary>
/// Cluster of users with similar preferences
/// </summary>
public class PreferenceCluster
{
    public string ClusterId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public UserPreferences TypicalPreferences { get; set; } = new();
    public List<string> CharacteristicBehaviors { get; private set; } = new();
    public int MemberCount { get; set; }
}

/// <summary>
/// Exportable learning model
/// </summary>
public class LearningModel
{
    public string ModelId { get; set; } = string.Empty;
    public string PersonaId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastUpdated { get; set; }
    public Dictionary<string, object> Parameters { get; private set; } = new();
    public Dictionary<string, double> Weights { get; private set; } = new();
    public List<LearningRule> Rules { get; private set; } = new();
    public string Version { get; set; } = "1.0";
}

/// <summary>
/// A learned rule for behavior adaptation
/// </summary>
public class LearningRule
{
    public string RuleId { get; set; } = string.Empty;
    public string Condition { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public int TimesApplied { get; set; }
    public double SuccessRate { get; set; }
}