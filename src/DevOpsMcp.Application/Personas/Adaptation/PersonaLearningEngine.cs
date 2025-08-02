using DevOpsMcp.Domain.Personas;
using DevOpsMcp.Domain.Personas.Adaptation;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DevOpsMcp.Application.Personas.Adaptation;

public class PersonaLearningEngine : IPersonaLearningEngine
{
    private readonly ILogger<PersonaLearningEngine> _logger;
    private readonly Dictionary<string, LearningModel> _models;
    private readonly Dictionary<string, UserPreferences> _userPreferencesCache;
    private readonly object _lockObject = new();

    public PersonaLearningEngine(ILogger<PersonaLearningEngine> logger)
    {
        _logger = logger;
        _models = new Dictionary<string, LearningModel>();
        _userPreferencesCache = new Dictionary<string, UserPreferences>();
    }

    public async Task<LearningInsights> ProcessInteractionAsync(
        UserInteraction interaction,
        PersonaResponse response)
    {
        _logger.LogDebug("Processing interaction {InteractionId} for learning", interaction.Id);

        var insights = new LearningInsights
        {
            InteractionId = interaction.Id,
            OverallEffectiveness = 0.5 // Base effectiveness
        };

        // Analyze communication effectiveness
        insights.CommunicationScore = AnalyzeCommunicationEffectiveness(interaction, response);
        
        // Analyze technical alignment
        insights.TechnicalAlignment = AnalyzeTechnicalAlignment(interaction, response);

        // Extract preference signals
        ExtractPreferenceSignals(interaction, response, insights);

        // Identify positive and negative indicators
        IdentifyIndicators(interaction, response, insights);

        // Calculate overall effectiveness
        insights.OverallEffectiveness = CalculateOverallEffectiveness(insights);

        _logger.LogInformation("Processed interaction with effectiveness score: {Score}", 
            insights.OverallEffectiveness);

        return await Task.FromResult(insights);
    }

    public async Task<UserPreferences> UpdateUserPreferencesAsync(
        string userId,
        LearningInsights insights,
        UserPreferences currentPreferences)
    {
        _logger.LogDebug("Updating preferences for user {UserId}", userId);

        var updatedPreferences = currentPreferences ?? new UserPreferences { UserId = userId };
        
        // Update communication preference based on insights
        if (insights.PreferenceSignals.TryGetValue("communication_style", out var commScore))
        {
            updatedPreferences.CommunicationPreference = DetermineCommunicationPreference(commScore);
        }

        // Update technical depth preference
        if (insights.TechnicalAlignment.JustRight)
        {
            // Keep current preference
        }
        else if (insights.TechnicalAlignment.TooSimple)
        {
            updatedPreferences.PreferredTechnicalDepth = IncreaseTechnicalDepth(updatedPreferences.PreferredTechnicalDepth);
        }
        else if (insights.TechnicalAlignment.TooComplex)
        {
            updatedPreferences.PreferredTechnicalDepth = DecreaseTechnicalDepth(updatedPreferences.PreferredTechnicalDepth);
        }

        // Update topic interest scores
        foreach (var signal in insights.PreferenceSignals.Where(s => s.Key.StartsWith("topic_", StringComparison.Ordinal)))
        {
            var topic = signal.Key.Replace("topic_", "");
            updatedPreferences.TopicInterestScores[topic] = 
                updatedPreferences.TopicInterestScores.GetValueOrDefault(topic, 0.5) * 0.7 + signal.Value * 0.3;
        }

        // Update response length preference
        if (insights.PreferenceSignals.TryGetValue("response_length", out var lengthPref))
        {
            updatedPreferences.PreferredResponseLength = DetermineResponseLength(lengthPref);
        }

        // Update other preferences
        if (insights.PreferenceSignals.TryGetValue("prefers_examples", out var examplePref))
        {
            updatedPreferences.PrefersExamples = examplePref > 0.5;
        }

        updatedPreferences.LastUpdated = DateTime.UtcNow;

        // Cache the updated preferences
        lock (_lockObject)
        {
            _userPreferencesCache[userId] = updatedPreferences;
        }

        return await Task.FromResult(updatedPreferences);
    }

    public async Task<UserPreferences> PredictInitialPreferencesAsync(
        UserProfile userProfile,
        ProjectContext context)
    {
        _logger.LogDebug("Predicting initial preferences for user {UserId}", userProfile.Id);

        var preferences = new UserPreferences
        {
            UserId = userProfile.Id,
            LastUpdated = DateTime.UtcNow
        };

        // Predict based on experience level
        preferences.PreferredTechnicalDepth = userProfile.Experience switch
        {
            ExperienceLevel.Junior => TechnicalDepth.Beginner,
            ExperienceLevel.MidLevel => TechnicalDepth.Intermediate,
            ExperienceLevel.Senior => TechnicalDepth.Advanced,
            ExperienceLevel.Lead or ExperienceLevel.Principal => TechnicalDepth.Expert,
            _ => TechnicalDepth.Intermediate
        };

        // Predict communication style based on role
        preferences.CommunicationPreference = userProfile.TeamRole switch
        {
            TeamRole.Developer => PreferredCommunicationStyle.Practical,
            TeamRole.Manager or TeamRole.Lead => PreferredCommunicationStyle.Conceptual,
            TeamRole.Architect => PreferredCommunicationStyle.Detailed,
            _ => PreferredCommunicationStyle.Standard
        };

        // Set default preferences
        preferences.PreferredResponseLength = ResponseLength.Standard;
        preferences.PrefersExamples = userProfile.Experience == ExperienceLevel.Junior;
        preferences.PrefersVisualAids = userProfile.TeamRole == TeamRole.Architect;

        // Initialize topic interests based on specialization
        InitializeTopicInterests(preferences, userProfile.Specialization);

        // Add preferred tools based on tech stack
        if (context.TechnologyStack?.RequiredFrameworks != null)
        {
            foreach (var framework in context.TechnologyStack.RequiredFrameworks)
            {
                preferences.PreferredTools.Add(framework);
            }
        }

        return await Task.FromResult(preferences);
    }

    public async Task<GlobalLearningPatterns> AnalyzeGlobalPatternsAsync()
    {
        _logger.LogInformation("Analyzing global learning patterns");

        var patterns = new GlobalLearningPatterns();

        // Analyze persona effectiveness across all models
        foreach (var model in _models)
        {
            var effectiveness = CalculatePersonaEffectiveness(model.Key, model.Value);
            patterns.PersonaEffectiveness[model.Key] = effectiveness;
        }

        // Identify common workflows
        var commonWorkflows = IdentifyCommonWorkflows();
        foreach (var workflow in commonWorkflows)
            patterns.CommonWorkflows[workflow.Key] = workflow.Value;

        // Identify frequent misunderstandings
        patterns.FrequentMisunderstandings.AddRange(IdentifyMisunderstandings());

        // Cluster user preferences
        var userClusters = ClusterUserPreferences();
        foreach (var cluster in userClusters)
            patterns.UserClusters[cluster.Key] = cluster.Value;

        return await Task.FromResult(patterns);
    }

    public async Task<LearningModel> ExportModelAsync(string personaId)
    {
        _logger.LogInformation("Exporting learning model for persona {PersonaId}", personaId);

        lock (_lockObject)
        {
            if (_models.TryGetValue(personaId, out var model))
            {
                // Create a deep copy for export
                var exportModel = new LearningModel
                {
                    ModelId = model.ModelId,
                    PersonaId = model.PersonaId,
                    CreatedAt = model.CreatedAt,
                    LastUpdated = model.LastUpdated,
                    Version = model.Version
                };

                foreach (var param in model.Parameters)
                    exportModel.Parameters[param.Key] = param.Value;

                foreach (var weight in model.Weights)
                    exportModel.Weights[weight.Key] = weight.Value;

                foreach (var rule in model.Rules)
                    exportModel.Rules.Add(rule);

                return exportModel;
            }
        }

        // Return empty model if not found
        await Task.CompletedTask;
        return new LearningModel { PersonaId = personaId };
    }

    public async Task ImportModelAsync(string personaId, LearningModel model)
    {
        _logger.LogInformation("Importing learning model for persona {PersonaId}", personaId);

        if (model == null || model.PersonaId != personaId)
        {
            throw new ArgumentException("Invalid model for import");
        }

        lock (_lockObject)
        {
            _models[personaId] = model;
        }

        await Task.CompletedTask;
    }

    private CommunicationEffectiveness AnalyzeCommunicationEffectiveness(
        UserInteraction interaction,
        PersonaResponse response)
    {
        var effectiveness = new CommunicationEffectiveness
        {
            Clarity = 0.7, // Base clarity
            Relevance = 0.7,
            Completeness = 0.7,
            Appropriateness = 0.7
        };

        // Analyze response length vs request complexity
        var requestComplexity = interaction.Request.Split(' ').Length;
        var responseLength = response.Response.Split(' ').Length;
        var ratio = responseLength / (double)Math.Max(requestComplexity, 1);

        if (ratio < 0.5)
            effectiveness.Completeness = 0.3; // Too brief
        else if (ratio > 20)
            effectiveness.Clarity = 0.4; // Too verbose

        // Check if response addresses the question
        if (response.Metadata.Topics.Any())
        {
            effectiveness.Relevance = 0.8;
        }

        // Assess appropriateness based on context
        if (interaction.Context.TryGetValue("urgency", out var urgencyObj) && 
            urgencyObj is string urgency && 
            urgency == "high")
        {
            // For urgent requests, concise responses are more appropriate
            effectiveness.Appropriateness = ratio < 5 ? 0.9 : 0.5;
        }

        return effectiveness;
    }

    private TechnicalAlignmentScore AnalyzeTechnicalAlignment(
        UserInteraction interaction,
        PersonaResponse response)
    {
        var alignment = new TechnicalAlignmentScore
        {
            AlignmentScore = 0.7 // Base alignment
        };

        // Simple heuristic: count technical terms
        var technicalTermsInRequest = CountTechnicalTerms(interaction.Request);
        var technicalTermsInResponse = CountTechnicalTerms(response.Response);

        var ratio = technicalTermsInResponse / (double)Math.Max(technicalTermsInRequest, 1);

        if (ratio < 0.5)
        {
            alignment.TooSimple = true;
            alignment.AlignmentScore = 0.4;
        }
        else if (ratio > 3)
        {
            alignment.TooComplex = true;
            alignment.AlignmentScore = 0.4;
            
            // Identify concepts that might be too advanced
            var advancedConcepts = new[] { "orchestration", "service mesh", "event sourcing", 
                "saga pattern", "circuit breaker", "bulkhead" };
            
            foreach (var concept in advancedConcepts)
            {
                if (response.Response.Contains(concept, StringComparison.OrdinalIgnoreCase) &&
                    !interaction.Request.Contains(concept, StringComparison.OrdinalIgnoreCase))
                {
                    alignment.MisalignedConcepts.Add(concept);
                }
            }
        }
        else
        {
            alignment.JustRight = true;
            alignment.AlignmentScore = 0.9;
        }

        return alignment;
    }

    private void ExtractPreferenceSignals(
        UserInteraction interaction,
        PersonaResponse response,
        LearningInsights insights)
    {
        // Communication style signal
        if (interaction.Request.Length < 50)
            insights.PreferenceSignals["communication_style"] = -0.5; // Prefers concise
        else if (interaction.Request.Length > 200)
            insights.PreferenceSignals["communication_style"] = 0.5; // Prefers detailed

        // Response length preference
        insights.PreferenceSignals["response_length"] = response.Response.Length switch
        {
            < 100 => -0.5,
            > 1000 => 0.5,
            _ => 0
        };

        // Topic interests based on metadata
        foreach (var topic in response.Metadata.Topics)
        {
            insights.PreferenceSignals[$"topic_{topic.ToLowerInvariant()}"] = 0.7;
        }

        // Example preference
        if (response.Response.Contains("example", StringComparison.OrdinalIgnoreCase) ||
            response.Response.Contains("for instance", StringComparison.OrdinalIgnoreCase))
        {
            insights.PreferenceSignals["prefers_examples"] = 0.8;
        }
    }

    private void IdentifyIndicators(
        UserInteraction interaction,
        PersonaResponse response,
        LearningInsights insights)
    {
        // Positive indicators
        if (interaction.Type == InteractionType.FollowUp)
        {
            insights.PositiveIndicators.Add("User engaged with follow-up question");
        }

        if (response.Confidence.Overall > 0.8)
        {
            insights.PositiveIndicators.Add("High confidence response");
        }

        if (response.SuggestedActions.Any())
        {
            insights.PositiveIndicators.Add("Provided actionable suggestions");
        }

        // Negative indicators
        if (interaction.Type == InteractionType.Clarification)
        {
            insights.NegativeIndicators.Add("User needed clarification");
        }

        if (interaction.Duration > 10)
        {
            insights.NegativeIndicators.Add("Long interaction duration");
        }

        if (response.Confidence.Caveats.Any())
        {
            insights.NegativeIndicators.Add($"Response had caveats: {response.Confidence.Caveats.Count}");
        }
    }

    private double CalculateOverallEffectiveness(LearningInsights insights)
    {
        var effectiveness = insights.CommunicationScore.Overall * 0.3 +
                          insights.TechnicalAlignment.AlignmentScore * 0.3 +
                          (insights.PositiveIndicators.Count / 5.0) * 0.2 +
                          (1 - insights.NegativeIndicators.Count / 5.0) * 0.2;

        return Math.Max(0, Math.Min(1, effectiveness));
    }

    private PreferredCommunicationStyle DetermineCommunicationPreference(double score)
    {
        return score switch
        {
            < -0.5 => PreferredCommunicationStyle.Concise,
            < -0.2 => PreferredCommunicationStyle.Practical,
            < 0.2 => PreferredCommunicationStyle.Standard,
            < 0.5 => PreferredCommunicationStyle.Detailed,
            _ => PreferredCommunicationStyle.Conceptual
        };
    }

    private ResponseLength DetermineResponseLength(double score)
    {
        return score switch
        {
            < -0.3 => ResponseLength.Brief,
            < 0.3 => ResponseLength.Standard,
            _ => ResponseLength.Comprehensive
        };
    }

    private TechnicalDepth IncreaseTechnicalDepth(TechnicalDepth current)
    {
        return current switch
        {
            TechnicalDepth.Beginner => TechnicalDepth.Intermediate,
            TechnicalDepth.Intermediate => TechnicalDepth.Advanced,
            TechnicalDepth.Advanced => TechnicalDepth.Expert,
            _ => current
        };
    }

    private TechnicalDepth DecreaseTechnicalDepth(TechnicalDepth current)
    {
        return current switch
        {
            TechnicalDepth.Expert => TechnicalDepth.Advanced,
            TechnicalDepth.Advanced => TechnicalDepth.Intermediate,
            TechnicalDepth.Intermediate => TechnicalDepth.Beginner,
            _ => current
        };
    }

    private void InitializeTopicInterests(UserPreferences preferences, string specialization)
    {
        var topicScores = specialization switch
        {
            "Infrastructure" => new Dictionary<string, double>
            {
                ["infrastructure"] = 0.9,
                ["automation"] = 0.8,
                ["cloud"] = 0.8,
                ["networking"] = 0.7
            },
            "Development" => new Dictionary<string, double>
            {
                ["development"] = 0.9,
                ["ci/cd"] = 0.8,
                ["testing"] = 0.7,
                ["deployment"] = 0.8
            },
            "Security" => new Dictionary<string, double>
            {
                ["security"] = 0.9,
                ["compliance"] = 0.8,
                ["identity"] = 0.7,
                ["encryption"] = 0.7
            },
            _ => new Dictionary<string, double> { ["general"] = 0.7 }
        };

        foreach (var score in topicScores)
        {
            preferences.TopicInterestScores[score.Key] = score.Value;
        }
    }

    private int CountTechnicalTerms(string text)
    {
        var technicalTerms = new[]
        {
            "api", "deployment", "container", "kubernetes", "docker", "pipeline",
            "infrastructure", "cloud", "server", "database", "cache", "queue",
            "microservice", "architecture", "framework", "library", "sdk"
        };

        var lowerText = text.ToLowerInvariant();
        return technicalTerms.Count(term => lowerText.Contains(term));
    }

    private PersonaEffectiveness CalculatePersonaEffectiveness(string personaId, LearningModel model)
    {
        var effectiveness = new PersonaEffectiveness
        {
            PersonaId = personaId,
            OverallSatisfaction = model.Weights.GetValueOrDefault("overall_satisfaction", 0.7)
        };

        // Calculate category effectiveness from model data
        foreach (TaskCategory category in Enum.GetValues<TaskCategory>())
        {
            var key = $"category_{category}";
            if (model.Weights.TryGetValue(key, out var score))
            {
                effectiveness.CategoryEffectiveness[category] = score;
            }
        }

        // Identify strengths and improvement areas
        var allScores = model.Weights.Where(w => w.Key.StartsWith("skill_", StringComparison.Ordinal))
            .OrderByDescending(w => w.Value)
            .ToList();

        foreach (var score in allScores.Take(3))
        {
            effectiveness.StrengthAreas[score.Key.Replace("skill_", "")] = score.Value;
        }

        foreach (var score in allScores.TakeLast(3))
        {
            effectiveness.ImprovementAreas[score.Key.Replace("skill_", "")] = score.Value;
        }

        effectiveness.TotalInteractions = (int)model.Parameters.GetValueOrDefault("total_interactions", 0);

        return effectiveness;
    }

    private Dictionary<string, List<string>> IdentifyCommonWorkflows()
    {
        // Placeholder - would analyze interaction sequences
        return new Dictionary<string, List<string>>
        {
            ["deployment"] = new List<string> 
            { 
                "check_prerequisites", 
                "run_tests", 
                "build_artifacts", 
                "deploy_to_staging", 
                "validate", 
                "deploy_to_production" 
            },
            ["troubleshooting"] = new List<string>
            {
                "gather_symptoms",
                "check_logs",
                "identify_root_cause",
                "implement_fix",
                "verify_resolution"
            }
        };
    }

    private List<CommonMisunderstanding> IdentifyMisunderstandings()
    {
        // Placeholder - would analyze clarification patterns
        return new List<CommonMisunderstanding>
        {
            new CommonMisunderstanding
            {
                Pattern = "CI/CD vs CI+CD",
                Examples = { "continuous integration and deployment", "CI and CD separately" },
                RecommendedClarification = "CI/CD refers to the combined practice of Continuous Integration and Continuous Deployment/Delivery",
                Frequency = 0.15
            },
            new CommonMisunderstanding
            {
                Pattern = "Container vs VM",
                Examples = { "containers are lightweight VMs", "VMs and containers are the same" },
                RecommendedClarification = "Containers share the host OS kernel, while VMs have their own OS",
                Frequency = 0.12
            }
        };
    }

    private Dictionary<string, PreferenceCluster> ClusterUserPreferences()
    {
        // Placeholder - would use clustering algorithm on user preferences
        return new Dictionary<string, PreferenceCluster>
        {
            ["technical_experts"] = new PreferenceCluster
            {
                ClusterId = "technical_experts",
                Description = "Users who prefer deep technical details and advanced concepts",
                TypicalPreferences = new UserPreferences
                {
                    PreferredTechnicalDepth = TechnicalDepth.Expert,
                    CommunicationPreference = PreferredCommunicationStyle.Detailed,
                    PreferredResponseLength = ResponseLength.Comprehensive
                },
                CharacteristicBehaviors = { "Uses technical jargon", "Asks about implementation details", "Prefers code examples" },
                MemberCount = 127
            },
            ["pragmatic_practitioners"] = new PreferenceCluster
            {
                ClusterId = "pragmatic_practitioners",
                Description = "Users who want practical, actionable guidance",
                TypicalPreferences = new UserPreferences
                {
                    PreferredTechnicalDepth = TechnicalDepth.Intermediate,
                    CommunicationPreference = PreferredCommunicationStyle.Practical,
                    PreferredResponseLength = ResponseLength.Standard,
                    PrefersExamples = true
                },
                CharacteristicBehaviors = { "Asks 'how to' questions", "Focuses on implementation", "Values working examples" },
                MemberCount = 342
            }
        };
    }
}