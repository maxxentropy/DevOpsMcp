namespace DevOpsMcp.Domain.Personas;

public class PersonaConfiguration
{
    public CommunicationStyle CommunicationStyle { get; set; }
    public TechnicalDepth TechnicalDepth { get; set; }
    public RiskTolerance RiskTolerance { get; set; }
    public DecisionMakingStyle DecisionMakingStyle { get; set; }
    public CollaborationPreferences CollaborationPreferences { get; set; }
    public SecurityPosture SecurityPosture { get; set; }
    public ResponseFormat ResponseFormat { get; set; }
    public double ProactivityLevel { get; set; } = 0.5;
    public ContextAwareness ContextAwareness { get; set; }
    public CollaborationMode CollaborationMode { get; set; }
    public ErrorHandlingApproach ErrorHandlingApproach { get; set; }
    public LearningMode LearningMode { get; set; }
}

public enum CommunicationStyle
{
    Concise,
    TechnicalPrecise,
    BusinessOriented,
    Collaborative,
    Mentoring,
    Executive
}

public enum TechnicalDepth
{
    Beginner,
    Intermediate,
    Advanced,
    Expert
}

public enum RiskTolerance
{
    Conservative,
    Moderate,
    Aggressive
}

public enum DecisionMakingStyle
{
    DataDriven,
    Consensus,
    Authoritative,
    Experimental
}

public enum CollaborationPreferences
{
    Independent,
    CrossFunctional,
    TeamOriented,
    Leadership
}

public enum SecurityPosture
{
    Relaxed,
    Standard,
    Strict,
    Paranoid
}

public enum ResponseFormat
{
    Brief,
    Standard,
    Detailed,
    Structured,
    Tutorial
}

public enum ContextAwareness
{
    Low,
    Medium,
    High,
    Comprehensive
}

public enum CollaborationMode
{
    Independent,
    Supportive,
    Collaborative,
    Leading
}

public enum ErrorHandlingApproach
{
    Minimal,
    Defensive,
    Comprehensive,
    ProactivePreventive
}

public enum LearningMode
{
    Static,
    Adaptive,
    Continuous,
    Predictive
}