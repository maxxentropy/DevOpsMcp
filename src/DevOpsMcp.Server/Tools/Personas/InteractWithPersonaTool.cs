using DevOpsMcp.Domain.Personas;
using DevOpsMcp.Domain.Personas.Orchestration;
using DevOpsMcp.Server.Mcp;
using System.Text.Json;

namespace DevOpsMcp.Server.Tools.Personas;

/// <summary>
/// Tool for interacting with a specific persona or multiple personas
/// </summary>
public class InteractWithPersonaTool : BaseTool<InteractWithPersonaArguments>
{
    private readonly IPersonaOrchestrator _orchestrator;
    private readonly IPersonaMemoryManager _memoryManager;

    public InteractWithPersonaTool(
        IPersonaOrchestrator orchestrator,
        IPersonaMemoryManager memoryManager)
    {
        _orchestrator = orchestrator;
        _memoryManager = memoryManager;
    }

    public override string Name => "interact_with_persona";
    
    public override string Description => 
        "Interact with one or more DevOps personas. Send a request and receive specialized responses based on persona expertise.";

    public override JsonElement InputSchema => CreateSchema<InteractWithPersonaArguments>();

    protected override async Task<CallToolResponse> ExecuteInternalAsync(
        InteractWithPersonaArguments arguments, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Create context
            var context = await BuildContextAsync(arguments);

            if (arguments.PersonaIds.Count == 1)
            {
                // Single persona interaction
                var response = await _orchestrator.RouteRequestAsync(
                    arguments.PersonaIds[0], 
                    context, 
                    arguments.Request);

                // Store conversation context if session is provided
                if (!string.IsNullOrEmpty(arguments.SessionId))
                {
                    await UpdateConversationContextAsync(
                        arguments.PersonaIds[0], 
                        arguments.SessionId, 
                        arguments.Request, 
                        response);
                }

                return CreateJsonResponse(new
                {
                    personaId = arguments.PersonaIds[0],
                    response = response.Response,
                    confidence = response.Confidence,
                    suggestedActions = response.SuggestedActions,
                    context = response.Context,
                    metadata = response.Metadata
                });
            }
            else
            {
                // Multi-persona orchestration
                var result = await _orchestrator.OrchestrateMultiPersonaResponseAsync(
                    context,
                    arguments.Request,
                    arguments.PersonaIds);

                return CreateJsonResponse(new
                {
                    consolidatedResponse = result.ConsolidatedResponse,
                    contributions = result.Contributions.Select(c => new
                    {
                        personaId = c.PersonaId,
                        response = c.Response.Response,
                        weight = c.Weight,
                        type = c.Type.ToString()
                    }),
                    metrics = result.Metrics,
                    combinedContext = result.CombinedContext
                });
            }
        }
        catch (Exception ex)
        {
            return CreateErrorResponse($"Error during persona interaction: {ex.Message}");
        }
    }

    private async Task<DevOpsContext> BuildContextAsync(InteractWithPersonaArguments arguments)
    {
        var context = new DevOpsContext
        {
            Project = new ProjectMetadata
            {
                ProjectId = arguments.ProjectId ?? "default",
                Name = arguments.ProjectName ?? "Default Project",
                Stage = arguments.ProjectStage ?? "Development"
            },
            Environment = new EnvironmentContext
            {
                EnvironmentType = arguments.EnvironmentType ?? "Development",
                IsProduction = arguments.IsProduction
            }
        };

        // Add user profile if provided
        if (!string.IsNullOrEmpty(arguments.UserId))
        {
            context.User = new UserProfile
            {
                Id = arguments.UserId,
                Name = arguments.UserName ?? "Unknown",
                Role = arguments.UserRole ?? "Developer",
                ExperienceLevel = arguments.UserExperienceLevel ?? "Intermediate"
            };
        }

        // Retrieve previous context if session is provided
        if (!string.IsNullOrEmpty(arguments.SessionId) && arguments.PersonaIds.Count == 1)
        {
            var previousContext = await _memoryManager.RetrieveConversationContextAsync(
                arguments.PersonaIds[0], 
                arguments.SessionId);
            
            if (previousContext != null)
            {
                context.Session = new SessionContext
                {
                    SessionId = arguments.SessionId,
                    StartTime = previousContext.StartTime,
                    InteractionCount = previousContext.InteractionHistory.Count
                };
            }
        }

        return context;
    }

    private async Task UpdateConversationContextAsync(
        string personaId, 
        string sessionId, 
        string request, 
        PersonaResponse response)
    {
        var conversationContext = await _memoryManager.RetrieveConversationContextAsync(personaId, sessionId)
            ?? new ConversationContext
            {
                PersonaId = personaId,
                SessionId = sessionId
            };

        var interaction = new InteractionSummary
        {
            UserInput = request,
            PersonaResponse = response.Response,
            Intent = response.Metadata.IntentClassification,
            SentimentScore = 0.0, // Would need sentiment analysis
            WasSuccessful = !response.IsError
        };

        // Add topics
        foreach (var topic in response.Metadata.Topics)
        {
            interaction.TopicsDiscussed.Add(topic);
        }

        conversationContext.InteractionHistory.Add(interaction);
        conversationContext.LastInteraction = DateTime.UtcNow;

        await _memoryManager.StoreConversationContextAsync(personaId, conversationContext);
    }
}

public class InteractWithPersonaArguments
{
    /// <summary>
    /// The request or question to send to the persona(s)
    /// </summary>
    public string Request { get; set; } = string.Empty;
    
    /// <summary>
    /// List of persona IDs to interact with. If multiple, will orchestrate responses.
    /// </summary>
    public List<string> PersonaIds { get; private set; } = new();
    
    /// <summary>
    /// Session ID for conversation continuity (optional)
    /// </summary>
    public string? SessionId { get; set; }
    
    /// <summary>
    /// User ID for personalization (optional)
    /// </summary>
    public string? UserId { get; set; }
    
    /// <summary>
    /// User name (optional)
    /// </summary>
    public string? UserName { get; set; }
    
    /// <summary>
    /// User role (optional)
    /// </summary>
    public string? UserRole { get; set; }
    
    /// <summary>
    /// User experience level: 'Beginner', 'Intermediate', 'Advanced', 'Expert'
    /// </summary>
    public string? UserExperienceLevel { get; set; }
    
    /// <summary>
    /// Project ID for context
    /// </summary>
    public string? ProjectId { get; set; }
    
    /// <summary>
    /// Project name for context
    /// </summary>
    public string? ProjectName { get; set; }
    
    /// <summary>
    /// Project stage: 'Development', 'Testing', 'Staging', 'Production'
    /// </summary>
    public string? ProjectStage { get; set; }
    
    /// <summary>
    /// Environment type
    /// </summary>
    public string? EnvironmentType { get; set; }
    
    /// <summary>
    /// Whether this is a production environment
    /// </summary>
    public bool IsProduction { get; set; }
}