using DevOpsMcp.Domain.Personas;
using DevOpsMcp.Domain.Personas.Adaptation;
using Microsoft.Extensions.Logging;

namespace DevOpsMcp.Application.Personas.Adaptation;

public class PersonaBehaviorAdapter : IPersonaBehaviorAdapter
{
    private readonly ILogger<PersonaBehaviorAdapter> _logger;
    private readonly IPersonaLearningEngine _learningEngine;
    private readonly Dictionary<string, AdaptationStrategy> _strategies;

    public PersonaBehaviorAdapter(
        ILogger<PersonaBehaviorAdapter> logger,
        IPersonaLearningEngine learningEngine)
    {
        _logger = logger;
        _learningEngine = learningEngine;
        _strategies = InitializeAdaptationStrategies();
    }

    public async Task<BehaviorAdjustment> AnalyzeInteractionPatternAsync(
        string personaId,
        UserInteraction interaction,
        InteractionHistory history)
    {
        _logger.LogInformation("Analyzing interaction pattern for persona {PersonaId}", personaId);

        var adjustment = new BehaviorAdjustment
        {
            ConfidenceScore = 0.5 // Base confidence
        };

        // Analyze communication style preferences
        var communicationAnalysis = AnalyzeCommunicationPreferences(history);
        if (communicationAnalysis.HasClearPreference)
        {
            adjustment.SuggestedCommunicationStyle = communicationAnalysis.PreferredStyle;
            adjustment.ParameterAdjustments["communication_confidence"] = communicationAnalysis.Confidence;
            adjustment.Reasons.Add($"User prefers {communicationAnalysis.PreferredStyle} communication style");
        }

        // Analyze technical depth preferences
        var technicalAnalysis = AnalyzeTechnicalDepthPreferences(history, interaction);
        if (technicalAnalysis.HasClearPreference)
        {
            adjustment.SuggestedTechnicalDepth = technicalAnalysis.PreferredDepth;
            adjustment.ParameterAdjustments["technical_confidence"] = technicalAnalysis.Confidence;
            adjustment.Reasons.Add($"User operates at {technicalAnalysis.PreferredDepth} technical level");
        }

        // Analyze response time patterns
        var responseTimeAnalysis = AnalyzeResponseTimePatterns(history);
        adjustment.ParameterAdjustments["preferred_response_time"] = responseTimeAnalysis.PreferredResponseTime;
        
        // Analyze interaction frequency
        var frequencyAnalysis = AnalyzeInteractionFrequency(history);
        adjustment.ParameterAdjustments["interaction_frequency"] = frequencyAnalysis.AverageFrequency;

        // Calculate overall confidence
        adjustment.ConfidenceScore = CalculateOverallConfidence(
            communicationAnalysis.Confidence,
            technicalAnalysis.Confidence,
            history.TotalInteractions);

        return await Task.FromResult(adjustment);
    }

    public async Task<PersonaConfiguration> AdaptConfigurationAsync(
        string personaId,
        PersonaConfiguration currentConfig,
        UserPreferences preferences,
        ProjectContext context)
    {
        _logger.LogInformation("Adapting configuration for persona {PersonaId}", personaId);

        var adaptedConfig = new PersonaConfiguration
        {
            // Start with current configuration
            CommunicationStyle = currentConfig.CommunicationStyle,
            TechnicalDepth = currentConfig.TechnicalDepth,
            RiskTolerance = currentConfig.RiskTolerance,
            DecisionMakingStyle = currentConfig.DecisionMakingStyle,
            CollaborationPreferences = currentConfig.CollaborationPreferences,
            SecurityPosture = currentConfig.SecurityPosture
        };

        // Apply user preference adaptations
        if (preferences.CommunicationPreference != PreferredCommunicationStyle.Standard)
        {
            adaptedConfig.CommunicationStyle = MapCommunicationStyle(preferences.CommunicationPreference);
        }

        if (preferences.PreferredTechnicalDepth != currentConfig.TechnicalDepth)
        {
            adaptedConfig.TechnicalDepth = preferences.PreferredTechnicalDepth;
        }

        // Apply context-based adaptations
        if (context.CurrentPhase == "Production" || context.CurrentPhase == "Critical")
        {
            adaptedConfig.RiskTolerance = RiskTolerance.Conservative;
            adaptedConfig.SecurityPosture = SecurityPosture.Strict;
        }

        // Apply role-based adaptations
        if (_strategies.TryGetValue(personaId, out var strategy))
        {
            adaptedConfig = strategy.ApplyRoleSpecificAdaptations(adaptedConfig, preferences, context);
        }

        return await Task.FromResult(adaptedConfig);
    }

    public async Task<double> CalculateAdaptationConfidenceAsync(
        string personaId,
        InteractionHistory history)
    {
        var confidence = 0.0;

        // Base confidence on interaction count
        if (history.TotalInteractions >= 50)
            confidence = 0.9;
        else if (history.TotalInteractions >= 20)
            confidence = 0.7;
        else if (history.TotalInteractions >= 10)
            confidence = 0.5;
        else if (history.TotalInteractions >= 5)
            confidence = 0.3;
        else
            confidence = 0.1;

        // Adjust based on feedback quality
        var positiveFeedback = history.Feedback.Count(f => f.Type == FeedbackType.Positive);
        var totalFeedback = history.Feedback.Count;
        
        if (totalFeedback > 0)
        {
            var feedbackRatio = (double)positiveFeedback / totalFeedback;
            confidence = confidence * 0.7 + feedbackRatio * 0.3;
        }

        // Adjust based on consistency of interactions
        var consistencyScore = CalculateConsistencyScore(history);
        confidence = confidence * 0.8 + consistencyScore * 0.2;

        return await Task.FromResult(Math.Min(1.0, confidence));
    }

    public async Task LearnFromFeedbackAsync(
        string personaId,
        UserFeedback feedback,
        PersonaResponse response)
    {
        _logger.LogInformation("Learning from feedback for persona {PersonaId}, feedback type: {Type}", 
            personaId, feedback.Type);

        // Create a learning context from the feedback
        var learningContext = new LearningContext
        {
            PersonaId = personaId,
            ResponseId = response.ResponseId,
            FeedbackType = feedback.Type,
            Rating = feedback.Rating,
            ResponseMetadata = response.Metadata
        };

        // Extract learning signals
        var signals = ExtractLearningSignals(feedback, response);

        // Update adaptation rules based on feedback
        if (feedback.Type == FeedbackType.Positive)
        {
            await ReinforceBehaviorAsync(personaId, response.Metadata, signals);
        }
        else if (feedback.Type == FeedbackType.Negative || feedback.Type == FeedbackType.Correction)
        {
            await AdjustBehaviorAsync(personaId, response.Metadata, signals, feedback.ImprovementSuggestions);
        }

        // Log learning event
        _logger.LogDebug("Processed {SignalCount} learning signals from feedback", signals.Count);
    }

    private Dictionary<string, AdaptationStrategy> InitializeAdaptationStrategies()
    {
        return new Dictionary<string, AdaptationStrategy>
        {
            ["devops-engineer"] = new DevOpsEngineerAdaptationStrategy(),
            ["sre-specialist"] = new SREAdaptationStrategy(),
            ["security-engineer"] = new SecurityEngineerAdaptationStrategy(),
            ["engineering-manager"] = new EngineeringManagerAdaptationStrategy()
        };
    }

    private CommunicationAnalysis AnalyzeCommunicationPreferences(InteractionHistory history)
    {
        var analysis = new CommunicationAnalysis();
        
        if (history.RecentInteractions.Count < 3)
        {
            analysis.HasClearPreference = false;
            return analysis;
        }

        // Analyze request patterns
        var avgRequestLength = history.RecentInteractions.Average(i => i.Request.Length);
        var usesTerminology = history.RecentInteractions.Count(i => ContainsTechnicalTerms(i.Request)) / (double)history.RecentInteractions.Count;

        if (avgRequestLength < 50 && usesTerminology < 0.3)
        {
            analysis.PreferredStyle = CommunicationStyle.Concise;
            analysis.Confidence = 0.7;
        }
        else if (avgRequestLength > 200 || usesTerminology > 0.7)
        {
            analysis.PreferredStyle = CommunicationStyle.TechnicalPrecise;
            analysis.Confidence = 0.8;
        }
        else
        {
            analysis.PreferredStyle = CommunicationStyle.Collaborative;
            analysis.Confidence = 0.6;
        }

        analysis.HasClearPreference = analysis.Confidence > 0.5;
        return analysis;
    }

    private TechnicalAnalysis AnalyzeTechnicalDepthPreferences(InteractionHistory history, UserInteraction currentInteraction)
    {
        var analysis = new TechnicalAnalysis();
        
        // Count technical concepts in recent interactions
        var technicalComplexity = history.RecentInteractions
            .Select(i => CountTechnicalConcepts(i.Request))
            .Average();

        if (technicalComplexity < 1)
        {
            analysis.PreferredDepth = TechnicalDepth.Beginner;
            analysis.Confidence = 0.7;
        }
        else if (technicalComplexity < 3)
        {
            analysis.PreferredDepth = TechnicalDepth.Intermediate;
            analysis.Confidence = 0.8;
        }
        else if (technicalComplexity < 5)
        {
            analysis.PreferredDepth = TechnicalDepth.Advanced;
            analysis.Confidence = 0.8;
        }
        else
        {
            analysis.PreferredDepth = TechnicalDepth.Expert;
            analysis.Confidence = 0.9;
        }

        analysis.HasClearPreference = true;
        return analysis;
    }

    private ResponseTimeAnalysis AnalyzeResponseTimePatterns(InteractionHistory history)
    {
        return new ResponseTimeAnalysis
        {
            PreferredResponseTime = history.RecentInteractions.Count > 0 
                ? history.RecentInteractions.Average(i => i.Duration)
                : 2.0 // Default 2 seconds
        };
    }

    private InteractionFrequencyAnalysis AnalyzeInteractionFrequency(InteractionHistory history)
    {
        if (history.RecentInteractions.Count < 2)
        {
            return new InteractionFrequencyAnalysis { AverageFrequency = 0 };
        }

        var timeDiffs = new List<double>();
        for (int i = 1; i < history.RecentInteractions.Count; i++)
        {
            var diff = (history.RecentInteractions[i].Timestamp - history.RecentInteractions[i - 1].Timestamp).TotalMinutes;
            timeDiffs.Add(diff);
        }

        return new InteractionFrequencyAnalysis
        {
            AverageFrequency = timeDiffs.Count > 0 ? timeDiffs.Average() : 0
        };
    }

    private double CalculateOverallConfidence(double communicationConfidence, double technicalConfidence, int totalInteractions)
    {
        var baseConfidence = Math.Min(1.0, totalInteractions / 50.0);
        return (baseConfidence * 0.4 + communicationConfidence * 0.3 + technicalConfidence * 0.3);
    }

    private double CalculateConsistencyScore(InteractionHistory history)
    {
        if (history.PersonaUsageCount.Count == 0)
            return 0.5;

        var totalUsage = history.PersonaUsageCount.Values.Sum();
        var maxUsage = history.PersonaUsageCount.Values.Max();
        
        return (double)maxUsage / totalUsage;
    }

    private CommunicationStyle MapCommunicationStyle(PreferredCommunicationStyle preference)
    {
        return preference switch
        {
            PreferredCommunicationStyle.Concise => CommunicationStyle.Concise,
            PreferredCommunicationStyle.Standard => CommunicationStyle.BusinessOriented,
            PreferredCommunicationStyle.Detailed => CommunicationStyle.TechnicalPrecise,
            PreferredCommunicationStyle.StepByStep => CommunicationStyle.Mentoring,
            PreferredCommunicationStyle.Conceptual => CommunicationStyle.Executive,
            PreferredCommunicationStyle.Practical => CommunicationStyle.Collaborative,
            _ => CommunicationStyle.Collaborative
        };
    }

    private bool ContainsTechnicalTerms(string text)
    {
        var technicalTerms = new[] { "api", "deployment", "pipeline", "infrastructure", "container", 
            "kubernetes", "docker", "ci/cd", "terraform", "azure", "aws", "cloud" };
        var lowerText = text.ToLowerInvariant();
        return technicalTerms.Any(term => lowerText.Contains(term));
    }

    private int CountTechnicalConcepts(string text)
    {
        var concepts = new[] { "deployment", "pipeline", "infrastructure", "container", "orchestration",
            "monitoring", "scaling", "security", "authentication", "authorization", "api", "microservice",
            "database", "cache", "queue", "load balancer", "proxy", "firewall", "vpc", "subnet" };
        
        var lowerText = text.ToLowerInvariant();
        return concepts.Count(concept => lowerText.Contains(concept));
    }

    private Dictionary<string, double> ExtractLearningSignals(UserFeedback feedback, PersonaResponse response)
    {
        var signals = new Dictionary<string, double>();

        // Extract rating signal
        signals["rating"] = feedback.Rating;

        // Extract response length preference
        if (feedback.Comment.Contains("too long", StringComparison.OrdinalIgnoreCase))
            signals["length_preference"] = -1;
        else if (feedback.Comment.Contains("too short", StringComparison.OrdinalIgnoreCase))
            signals["length_preference"] = 1;
        else
            signals["length_preference"] = 0;

        // Extract technical level preference
        if (feedback.Comment.Contains("too technical", StringComparison.OrdinalIgnoreCase))
            signals["technical_preference"] = -1;
        else if (feedback.Comment.Contains("too simple", StringComparison.OrdinalIgnoreCase))
            signals["technical_preference"] = 1;
        else
            signals["technical_preference"] = 0;

        return signals;
    }

    private async Task ReinforceBehaviorAsync(string personaId, ResponseMetadata metadata, Dictionary<string, double> signals)
    {
        // Reinforce positive behaviors
        _logger.LogDebug("Reinforcing positive behavior for persona {PersonaId}", personaId);
        await Task.CompletedTask;
    }

    private async Task AdjustBehaviorAsync(string personaId, ResponseMetadata metadata, 
        Dictionary<string, double> signals, List<string> suggestions)
    {
        // Adjust behaviors based on negative feedback
        _logger.LogDebug("Adjusting behavior for persona {PersonaId} based on {SuggestionCount} suggestions", 
            personaId, suggestions.Count);
        await Task.CompletedTask;
    }

    // Helper classes
    private sealed class CommunicationAnalysis
    {
        public bool HasClearPreference { get; set; }
        public CommunicationStyle PreferredStyle { get; set; }
        public double Confidence { get; set; }
    }

    private sealed class TechnicalAnalysis
    {
        public bool HasClearPreference { get; set; }
        public TechnicalDepth PreferredDepth { get; set; }
        public double Confidence { get; set; }
    }

    private sealed class ResponseTimeAnalysis
    {
        public double PreferredResponseTime { get; set; }
    }

    private sealed class InteractionFrequencyAnalysis
    {
        public double AverageFrequency { get; set; }
    }

    private sealed class LearningContext
    {
        public string PersonaId { get; set; } = string.Empty;
        public string ResponseId { get; set; } = string.Empty;
        public FeedbackType FeedbackType { get; set; }
        public double Rating { get; set; }
        public ResponseMetadata ResponseMetadata { get; set; } = new();
    }
}

// Base class for persona-specific adaptation strategies
public abstract class AdaptationStrategy
{
    public abstract PersonaConfiguration ApplyRoleSpecificAdaptations(
        PersonaConfiguration config,
        UserPreferences preferences,
        ProjectContext context);
}

// DevOps Engineer specific adaptations
public class DevOpsEngineerAdaptationStrategy : AdaptationStrategy
{
    public override PersonaConfiguration ApplyRoleSpecificAdaptations(
        PersonaConfiguration config,
        UserPreferences preferences,
        ProjectContext context)
    {
        // DevOps engineers might prefer more experimental approaches in dev environments
        if (context.CurrentPhase == "Development" && preferences.TopicInterestScores.GetValueOrDefault("automation") > 0.7)
        {
            config.DecisionMakingStyle = DecisionMakingStyle.Experimental;
            config.RiskTolerance = RiskTolerance.Moderate;
        }

        return config;
    }
}

// SRE specific adaptations
public class SREAdaptationStrategy : AdaptationStrategy
{
    public override PersonaConfiguration ApplyRoleSpecificAdaptations(
        PersonaConfiguration config,
        UserPreferences preferences,
        ProjectContext context)
    {
        // SREs are always data-driven and conservative in production
        config.DecisionMakingStyle = DecisionMakingStyle.DataDriven;
        
        if (context.CurrentPhase == "Production")
        {
            config.RiskTolerance = RiskTolerance.Conservative;
            config.SecurityPosture = SecurityPosture.Strict;
        }

        return config;
    }
}

// Security Engineer specific adaptations
public class SecurityEngineerAdaptationStrategy : AdaptationStrategy
{
    public override PersonaConfiguration ApplyRoleSpecificAdaptations(
        PersonaConfiguration config,
        UserPreferences preferences,
        ProjectContext context)
    {
        // Security engineers maintain strict posture regardless
        config.SecurityPosture = SecurityPosture.Strict;
        config.RiskTolerance = RiskTolerance.Conservative;
        
        return config;
    }
}

// Engineering Manager specific adaptations
public class EngineeringManagerAdaptationStrategy : AdaptationStrategy
{
    public override PersonaConfiguration ApplyRoleSpecificAdaptations(
        PersonaConfiguration config,
        UserPreferences preferences,
        ProjectContext context)
    {
        // Managers adapt communication based on audience
        if (preferences.CommunicationPreference == PreferredCommunicationStyle.Conceptual)
        {
            config.CommunicationStyle = CommunicationStyle.Executive;
        }
        else if (preferences.PrefersExamples)
        {
            config.CommunicationStyle = CommunicationStyle.Mentoring;
        }

        return config;
    }
}