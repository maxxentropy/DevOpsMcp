using DevOpsMcp.Domain.Personas;
using Microsoft.Extensions.Logging;

namespace DevOpsMcp.Application.Personas;

public abstract class BaseDevOpsPersona : IDevOpsPersona
{
    private readonly ILogger<BaseDevOpsPersona> _logger;
    private readonly IPersonaMemoryManager _memoryManager;
    private PersonaConfiguration _currentConfiguration;

    protected BaseDevOpsPersona(
        ILogger<BaseDevOpsPersona> logger,
        IPersonaMemoryManager memoryManager)
    {
        _logger = logger;
        _memoryManager = memoryManager;
        _currentConfiguration = new PersonaConfiguration(); // Initialize with default
        Capabilities = new Dictionary<string, object>(); // Initialize empty
    }

    public abstract string Id { get; }
    public abstract string Name { get; }
    public abstract string Role { get; }
    public abstract string Description { get; }
    public abstract DevOpsSpecialization Specialization { get; }
    
    public Dictionary<string, object> Capabilities { get; private set; }
    public PersonaConfiguration Configuration => _currentConfiguration;
    
    protected void Initialize()
    {
        _currentConfiguration = GetDefaultConfiguration();
        Capabilities = InitializeCapabilities();
    }

    public virtual async Task<PersonaResponse> ProcessRequestAsync(DevOpsContext context, string request)
    {
        _logger.LogInformation("Processing request for persona {PersonaId}: {Request}", Id, request);

        try
        {
            // Store interaction context
            var conversationContext = await GetOrCreateConversationContext(context);
            
            // Analyze request
            var analysis = await AnalyzeRequestAsync(request, context);
            
            // Generate response based on persona characteristics
            var response = await GenerateResponseAsync(analysis, context);
            
            // Update conversation memory
            await UpdateConversationMemory(conversationContext, request, response);
            
            // Apply persona-specific formatting
            response = ApplyPersonaFormatting(response, context);
            
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing request in persona {PersonaId}", Id);
            return CreateErrorResponse(ex, context);
        }
    }

    public virtual async Task<double> CalculateRoleAlignmentAsync(DevOpsTask task)
    {
        var score = 0.0;
        var weights = GetAlignmentWeights();
        
        // Category alignment
        var categoryScore = CalculateCategoryAlignment(task.Category);
        score += categoryScore * weights["category"];
        
        // Skill alignment
        var skillScore = CalculateSkillAlignment(task.RequiredSkills);
        score += skillScore * weights["skills"];
        
        // Complexity alignment
        var complexityScore = CalculateComplexityAlignment(task.Complexity);
        score += complexityScore * weights["complexity"];
        
        // Specialization alignment
        var specializationScore = CalculateSpecializationAlignment(task);
        score += specializationScore * weights["specialization"];
        
        return Math.Min(1.0, Math.Max(0.0, score));
    }

    public virtual async Task AdaptBehaviorAsync(UserProfile userProfile, ProjectContext projectContext)
    {
        _logger.LogInformation("Adapting behavior for persona {PersonaId}", Id);
        
        _currentConfiguration = new PersonaConfiguration
        {
            CommunicationStyle = AdaptCommunicationStyle(userProfile, projectContext),
            TechnicalDepth = AdaptTechnicalDepth(userProfile),
            RiskTolerance = AdaptRiskTolerance(userProfile, projectContext),
            DecisionMakingStyle = AdaptDecisionMakingStyle(projectContext),
            CollaborationPreferences = AdaptCollaborationPreferences(userProfile),
            SecurityPosture = AdaptSecurityPosture(projectContext)
        };
        
        await Task.CompletedTask;
    }

    protected abstract PersonaConfiguration GetDefaultConfiguration();
    protected abstract Dictionary<string, object> InitializeCapabilities();
    protected abstract Task<RequestAnalysis> AnalyzeRequestAsync(string request, DevOpsContext context);
    protected abstract Task<PersonaResponse> GenerateResponseAsync(RequestAnalysis analysis, DevOpsContext context);
    protected abstract Dictionary<string, double> GetAlignmentWeights();
    
    protected virtual CommunicationStyle AdaptCommunicationStyle(UserProfile userProfile, ProjectContext projectContext)
    {
        return userProfile.Experience switch
        {
            ExperienceLevel.Junior => CommunicationStyle.Mentoring,
            ExperienceLevel.Senior or ExperienceLevel.Lead => CommunicationStyle.Collaborative,
            ExperienceLevel.Principal => CommunicationStyle.Executive,
            _ => Configuration.CommunicationStyle
        };
    }

    protected virtual TechnicalDepth AdaptTechnicalDepth(UserProfile userProfile)
    {
        return userProfile.Experience switch
        {
            ExperienceLevel.Junior => TechnicalDepth.Beginner,
            ExperienceLevel.MidLevel => TechnicalDepth.Intermediate,
            ExperienceLevel.Senior => TechnicalDepth.Advanced,
            ExperienceLevel.Lead or ExperienceLevel.Principal => TechnicalDepth.Expert,
            _ => Configuration.TechnicalDepth
        };
    }

    protected virtual RiskTolerance AdaptRiskTolerance(UserProfile userProfile, ProjectContext projectContext)
    {
        if (projectContext.CurrentPhase == "Production" || projectContext.Constraints.Any(c => c.Severity == "Critical"))
        {
            return RiskTolerance.Conservative;
        }
        
        return userProfile.RiskProfile switch
        {
            RiskProfile.RiskAverse => RiskTolerance.Conservative,
            RiskProfile.RiskTolerant => RiskTolerance.Aggressive,
            _ => RiskTolerance.Moderate
        };
    }

    protected virtual DecisionMakingStyle AdaptDecisionMakingStyle(ProjectContext projectContext)
    {
        return projectContext.CurrentPhase switch
        {
            "Planning" => DecisionMakingStyle.Consensus,
            "Development" => DecisionMakingStyle.Experimental,
            "Production" => DecisionMakingStyle.DataDriven,
            _ => Configuration.DecisionMakingStyle
        };
    }

    protected virtual CollaborationPreferences AdaptCollaborationPreferences(UserProfile userProfile)
    {
        return userProfile.TeamRole switch
        {
            TeamRole.Manager or TeamRole.Lead => CollaborationPreferences.Leadership,
            TeamRole.Architect => CollaborationPreferences.CrossFunctional,
            TeamRole.Developer => CollaborationPreferences.TeamOriented,
            _ => Configuration.CollaborationPreferences
        };
    }

    protected virtual SecurityPosture AdaptSecurityPosture(ProjectContext projectContext)
    {
        var hasSecurityConstraints = projectContext.Constraints.Any(c => 
            c.Type.Contains("Security", StringComparison.OrdinalIgnoreCase) || 
            c.Type.Contains("Compliance", StringComparison.OrdinalIgnoreCase));
            
        return hasSecurityConstraints ? SecurityPosture.Strict : Configuration.SecurityPosture;
    }

    protected virtual PersonaResponse ApplyPersonaFormatting(PersonaResponse response, DevOpsContext context)
    {
        response.Metadata.Tone = Configuration.CommunicationStyle switch
        {
            CommunicationStyle.TechnicalPrecise => "Technical",
            CommunicationStyle.BusinessOriented => "Business",
            CommunicationStyle.Mentoring => "Educational",
            CommunicationStyle.Executive => "Strategic",
            _ => "Professional"
        };
        
        response.Metadata.ComplexityLevel = Configuration.TechnicalDepth switch
        {
            TechnicalDepth.Beginner => 1,
            TechnicalDepth.Intermediate => 2,
            TechnicalDepth.Advanced => 3,
            TechnicalDepth.Expert => 4,
            _ => 2
        };
        
        return response;
    }

    protected void SetResponseTopics(ResponseMetadata metadata, IEnumerable<string> topics)
    {
        metadata.Topics.Clear();
        if (topics != null)
        {
            foreach (var topic in topics)
            {
                metadata.Topics.Add(topic);
            }
        }
    }

    protected virtual double CalculateCategoryAlignment(TaskCategory category)
    {
        var alignmentMap = GetCategoryAlignmentMap();
        return alignmentMap.TryGetValue(category, out var score) ? score : 0.0;
    }

    protected virtual double CalculateSkillAlignment(List<string> requiredSkills)
    {
        if (!requiredSkills.Any()) return 1.0;
        
        var mySkills = GetPersonaSkills();
        var matchCount = requiredSkills.Count(skill => mySkills.Contains(skill, StringComparer.OrdinalIgnoreCase));
        return (double)matchCount / requiredSkills.Count;
    }

    protected virtual double CalculateComplexityAlignment(TaskComplexity complexity)
    {
        var maxComplexity = Configuration.TechnicalDepth switch
        {
            TechnicalDepth.Beginner => TaskComplexity.Simple,
            TechnicalDepth.Intermediate => TaskComplexity.Moderate,
            TechnicalDepth.Advanced => TaskComplexity.Complex,
            TechnicalDepth.Expert => TaskComplexity.Expert,
            _ => TaskComplexity.Moderate
        };
        
        return complexity <= maxComplexity ? 1.0 : 0.5;
    }

    protected virtual double CalculateSpecializationAlignment(DevOpsTask task)
    {
        // Override in specific personas
        return 0.5;
    }

    protected abstract Dictionary<TaskCategory, double> GetCategoryAlignmentMap();
    protected abstract List<string> GetPersonaSkills();

    private async Task<ConversationContext> GetOrCreateConversationContext(DevOpsContext context)
    {
        var sessionId = context.Project?.ProjectId ?? Guid.NewGuid().ToString();
        var existingContext = await _memoryManager.RetrieveConversationContextAsync(Id, sessionId);
        
        if (existingContext != null)
        {
            existingContext.LastInteraction = DateTime.UtcNow;
            return existingContext;
        }
        
        return new ConversationContext
        {
            SessionId = sessionId,
            PersonaId = Id,
            CurrentProjectState = new Domain.Personas.ProjectState
            {
                ProjectId = context.Project?.ProjectId ?? string.Empty,
                CurrentPhase = context.Project?.Stage ?? "Unknown"
            }
        };
    }

    private async Task UpdateConversationMemory(ConversationContext context, string request, PersonaResponse response)
    {
        var summary = new InteractionSummary
        {
            UserInput = request,
            PersonaResponse = response.Response,
            Intent = DetermineIntent(request),
            WasSuccessful = response.Confidence.Overall > 0.7
        };
        foreach (var topic in response.Metadata.Topics)
        {
            summary.TopicsDiscussed.Add(topic);
        }
        context.InteractionHistory.Add(summary);
        
        context.Metrics.TotalExchanges++;
        
        await _memoryManager.StoreConversationContextAsync(Id, context);
    }

    private string DetermineIntent(string request)
    {
        // Simple intent detection - can be enhanced with NLP
        var requestLower = request.ToLowerInvariant();
        
        if (requestLower.Contains("deploy")) return "Deployment";
        if (requestLower.Contains("error") || requestLower.Contains("issue")) return "Troubleshooting";
        if (requestLower.Contains("performance")) return "Performance";
        if (requestLower.Contains("security")) return "Security";
        if (requestLower.Contains("monitor")) return "Monitoring";
        if (requestLower.Contains("scale")) return "Scaling";
        
        return "General";
    }

    private PersonaResponse CreateErrorResponse(Exception ex, DevOpsContext context)
    {
        var response = new PersonaResponse
        {
            PersonaId = Id,
            Response = $"I encountered an issue while processing your request. {GetErrorGuidance(ex)}",
            Metadata = new ResponseMetadata
            {
                ResponseType = "Error",
                Tone = "Apologetic"
            },
            Confidence = new PersonaConfidence
            {
                Overall = 0.0
            }
        };
        response.Confidence.Caveats.Add("An error occurred during processing");
        return response;
    }

    private string GetErrorGuidance(Exception ex)
    {
        return Configuration.CommunicationStyle switch
        {
            CommunicationStyle.TechnicalPrecise => $"Technical details: {ex.Message}",
            CommunicationStyle.BusinessOriented => "Please try rephrasing your request or contact support.",
            CommunicationStyle.Mentoring => "This might be a temporary issue. Let's try a different approach.",
            _ => "Please try again or rephrase your request."
        };
    }
}

public class RequestAnalysis
{
    public string Intent { get; set; } = string.Empty;
    public Dictionary<string, object> Entities { get; private set; } = new();
    public List<string> Topics { get; private set; } = new();
    public double Urgency { get; set; }
    public TaskCategory EstimatedCategory { get; set; }
    public Dictionary<string, string> Context { get; private set; } = new();
    public double Confidence { get; set; }
}