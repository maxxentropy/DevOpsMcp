using DevOpsMcp.Domain.Personas;
using DevOpsMcp.Domain.Personas.Orchestration;
using DevOpsMcp.Server.Mcp;
using System.Text.Json;

namespace DevOpsMcp.Server.Tools.Personas;

/// <summary>
/// Tool for selecting the best persona for a given request
/// </summary>
public class SelectPersonaTool : BaseTool<SelectPersonaArguments>
{
    private readonly IPersonaOrchestrator _orchestrator;

    public SelectPersonaTool(IPersonaOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    public override string Name => "select_persona";
    
    public override string Description => 
        "Select the most appropriate persona(s) for a given DevOps request. Returns persona recommendations based on the context and request type.";

    public override JsonElement InputSchema => CreateSchema<SelectPersonaArguments>();

    protected override async Task<CallToolResponse> ExecuteInternalAsync(
        SelectPersonaArguments arguments, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Create context from arguments
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

            // Create selection criteria
            var criteria = new PersonaSelectionCriteria
            {
                SelectionMode = ParseSelectionMode(arguments.SelectionMode),
                MinimumConfidenceThreshold = arguments.MinimumConfidence ?? 0.7,
                AllowMultiplePersonas = arguments.AllowMultiple ?? false,
                MaxPersonaCount = arguments.MaxPersonaCount ?? 3
            };

            // Add preferred specializations if provided
            if (!string.IsNullOrEmpty(arguments.PreferredSpecialization))
            {
                var specialization = ParseSpecialization(arguments.PreferredSpecialization);
                criteria.PreferredSpecializations.Add(specialization);
            }

            // Select personas
            var result = await _orchestrator.SelectPersonaAsync(context, arguments.Request, criteria);

            // Format response
            var response = new
            {
                primaryPersona = new
                {
                    id = result.PrimaryPersonaId,
                    confidence = result.Confidence,
                    reason = result.SelectionReason
                },
                secondaryPersonas = result.SecondaryPersonaIds.Select(id => new
                {
                    id,
                    score = result.PersonaScores.GetValueOrDefault(id, 0.0)
                }),
                allScores = result.PersonaScores
            };

            return CreateJsonResponse(response);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse($"Error selecting persona: {ex.Message}");
        }
    }

    private PersonaSelectionMode ParseSelectionMode(string? mode)
    {
        return mode?.ToLowerInvariant() switch
        {
            "best_match" => PersonaSelectionMode.BestMatch,
            "round_robin" => PersonaSelectionMode.RoundRobin,
            "load_balanced" => PersonaSelectionMode.LoadBalanced,
            "specialization" => PersonaSelectionMode.SpecializationBased,
            "context_aware" => PersonaSelectionMode.ContextAware,
            _ => PersonaSelectionMode.BestMatch
        };
    }

    private DevOpsSpecialization ParseSpecialization(string specialization)
    {
        return specialization.ToLowerInvariant() switch
        {
            "infrastructure" => DevOpsSpecialization.Infrastructure,
            "development" => DevOpsSpecialization.Development,
            "security" => DevOpsSpecialization.Security,
            "reliability" => DevOpsSpecialization.Reliability,
            "management" => DevOpsSpecialization.Management,
            _ => DevOpsSpecialization.Development
        };
    }
}

public class SelectPersonaArguments
{
    /// <summary>
    /// The DevOps request or task description
    /// </summary>
    public string Request { get; set; } = string.Empty;
    
    /// <summary>
    /// Selection mode: 'best_match', 'round_robin', 'load_balanced', 'specialization', 'context_aware'
    /// </summary>
    public string? SelectionMode { get; set; }
    
    /// <summary>
    /// Minimum confidence threshold (0.0 - 1.0)
    /// </summary>
    public double? MinimumConfidence { get; set; }
    
    /// <summary>
    /// Whether to allow multiple personas to be selected
    /// </summary>
    public bool? AllowMultiple { get; set; }
    
    /// <summary>
    /// Maximum number of personas to select
    /// </summary>
    public int? MaxPersonaCount { get; set; }
    
    /// <summary>
    /// Preferred specialization: 'infrastructure', 'development', 'security', 'reliability', 'management'
    /// </summary>
    public string? PreferredSpecialization { get; set; }
    
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