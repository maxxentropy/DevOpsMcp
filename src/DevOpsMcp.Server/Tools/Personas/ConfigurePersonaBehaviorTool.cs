using DevOpsMcp.Application.Personas;
using DevOpsMcp.Domain.Personas;
using DevOpsMcp.Domain.Personas.Adaptation;
using DevOpsMcp.Server.Mcp;
using System.Text.Json;

namespace DevOpsMcp.Server.Tools.Personas;

/// <summary>
/// Tool for configuring persona behavior and adaptation settings
/// </summary>
public class ConfigurePersonaBehaviorTool : BaseTool<ConfigurePersonaBehaviorArguments>
{
    private readonly IPersonaBehaviorAdapter _behaviorAdapter;
    private readonly IServiceProvider _serviceProvider;

    public ConfigurePersonaBehaviorTool(
        IPersonaBehaviorAdapter behaviorAdapter,
        IServiceProvider serviceProvider)
    {
        _behaviorAdapter = behaviorAdapter;
        _serviceProvider = serviceProvider;
    }

    public override string Name => "configure_persona_behavior";
    
    public override string Description => 
        "Configure persona behavior settings including communication style, technical depth, response format, and adaptation preferences.";

    public override JsonElement InputSchema => CreateSchema<ConfigurePersonaBehaviorArguments>();

    protected override async Task<CallToolResponse> ExecuteInternalAsync(
        ConfigurePersonaBehaviorArguments arguments, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Get the persona
            var persona = GetPersona(arguments.PersonaId);
            if (persona == null)
            {
                return CreateErrorResponse($"Persona '{arguments.PersonaId}' not found");
            }

            // Build user preferences from arguments
            var userPreferences = BuildUserPreferences(arguments);

            // Build project context
            var projectContext = new ProjectContext
            {
                ProjectId = arguments.ProjectId ?? "default",
                ProjectName = arguments.ProjectName ?? "Default Project",
                Stage = arguments.ProjectStage ?? "Development"
            };

            // Adapt configuration
            var adaptedConfig = await _behaviorAdapter.AdaptConfigurationAsync(
                arguments.PersonaId,
                persona.Configuration,
                userPreferences,
                projectContext);

            // Update persona configuration
            await persona.AdaptBehaviorAsync(
                new UserProfile { Id = "system", Role = "Administrator" },
                projectContext);

            return CreateJsonResponse(new
            {
                personaId = arguments.PersonaId,
                updatedConfiguration = new
                {
                    communicationStyle = adaptedConfig.CommunicationStyle.ToString(),
                    technicalDepth = adaptedConfig.TechnicalDepth.ToString(),
                    responseFormat = adaptedConfig.ResponseFormat.ToString(),
                    proactivityLevel = adaptedConfig.ProactivityLevel,
                    contextAwareness = adaptedConfig.ContextAwareness.ToString(),
                    collaborationMode = adaptedConfig.CollaborationMode.ToString(),
                    errorHandling = adaptedConfig.ErrorHandlingApproach.ToString(),
                    learningMode = adaptedConfig.LearningMode.ToString()
                },
                appliedPreferences = userPreferences
            });
        }
        catch (Exception ex)
        {
            return CreateErrorResponse($"Error configuring persona behavior: {ex.Message}");
        }
    }

    private IDevOpsPersona? GetPersona(string personaId)
    {
        var personaType = personaId.ToLowerInvariant() switch
        {
            "devops-engineer" => typeof(DevOpsEngineerPersona),
            "sre-specialist" => typeof(SiteReliabilityEngineerPersona),
            "security-engineer" => typeof(SecurityEngineerPersona),
            "engineering-manager" => typeof(EngineeringManagerPersona),
            _ => null
        };

        if (personaType == null)
            return null;

        return _serviceProvider.GetService(personaType) as IDevOpsPersona;
    }

    private UserPreferences BuildUserPreferences(ConfigurePersonaBehaviorArguments arguments)
    {
        var preferences = new UserPreferences
        {
            CommunicationPreference = ParseCommunicationStyle(arguments.CommunicationStyle),
            PreferredResponseLength = ParseResponseLength(arguments.ResponseLength),
            PreferredTechnicalDepth = ParseTechnicalDepth(arguments.TechnicalLevel)
        };

        if (arguments.PreferredTools.Any())
        {
            foreach (var tool in arguments.PreferredTools)
            {
                preferences.PreferredTools.Add(tool);
            }
        }

        if (arguments.AvoidTopics.Any())
        {
            foreach (var topic in arguments.AvoidTopics)
            {
                preferences.AvoidedTopics.Add(topic);
            }
        }

        // Set example and visual preferences
        preferences.PrefersExamples = arguments.UseExamples;
        preferences.PrefersVisualAids = arguments.IncludeDiagrams;

        return preferences;
    }

    private PreferredCommunicationStyle ParseCommunicationStyle(string? style)
    {
        return style?.ToLowerInvariant() switch
        {
            "concise" => PreferredCommunicationStyle.Concise,
            "detailed" => PreferredCommunicationStyle.Detailed,
            "step_by_step" => PreferredCommunicationStyle.StepByStep,
            "conceptual" => PreferredCommunicationStyle.Conceptual,
            "practical" => PreferredCommunicationStyle.Practical,
            _ => PreferredCommunicationStyle.Standard
        };
    }

    private ResponseLength ParseResponseLength(string? length)
    {
        return length?.ToLowerInvariant() switch
        {
            "brief" => ResponseLength.Brief,
            "standard" => ResponseLength.Standard,
            "comprehensive" => ResponseLength.Comprehensive,
            _ => ResponseLength.Standard
        };
    }

    private TechnicalDepth ParseTechnicalDepth(string? level)
    {
        return level?.ToLowerInvariant() switch
        {
            "beginner" => TechnicalDepth.Beginner,
            "intermediate" => TechnicalDepth.Intermediate,
            "advanced" => TechnicalDepth.Advanced,
            "expert" => TechnicalDepth.Expert,
            _ => TechnicalDepth.Intermediate
        };
    }
}

public class ConfigurePersonaBehaviorArguments
{
    /// <summary>
    /// The ID of the persona to configure
    /// </summary>
    public string PersonaId { get; set; } = string.Empty;
    
    /// <summary>
    /// Preferred communication style: 'concise', 'detailed', 'step_by_step', 'conceptual', 'practical'
    /// </summary>
    public string? CommunicationStyle { get; set; }
    
    /// <summary>
    /// Preferred response length: 'brief', 'standard', 'comprehensive'
    /// </summary>
    public string? ResponseLength { get; set; }
    
    /// <summary>
    /// Technical expertise level: 'beginner', 'intermediate', 'advanced', 'expert'
    /// </summary>
    public string? TechnicalLevel { get; set; }
    
    /// <summary>
    /// List of preferred tools or technologies
    /// </summary>
    public List<string> PreferredTools { get; private set; } = new();
    
    /// <summary>
    /// List of topics to avoid
    /// </summary>
    public List<string> AvoidTopics { get; private set; } = new();
    
    /// <summary>
    /// Time zone for scheduling and time-based responses
    /// </summary>
    public string? TimeZone { get; set; }
    
    /// <summary>
    /// Preferred language
    /// </summary>
    public string? Language { get; set; }
    
    /// <summary>
    /// Whether to include examples in responses
    /// </summary>
    public bool UseExamples { get; set; } = true;
    
    /// <summary>
    /// Whether to include diagrams when relevant
    /// </summary>
    public bool IncludeDiagrams { get; set; }
    
    /// <summary>
    /// Project ID for context
    /// </summary>
    public string? ProjectId { get; set; }
    
    /// <summary>
    /// Project name for context
    /// </summary>
    public string? ProjectName { get; set; }
    
    /// <summary>
    /// Project stage for context
    /// </summary>
    public string? ProjectStage { get; set; }
}