using DevOpsMcp.Domain.Personas.Orchestration;
using DevOpsMcp.Server.Mcp;
using System.Text.Json;

namespace DevOpsMcp.Server.Tools.Personas;

/// <summary>
/// Tool for activating or deactivating personas
/// </summary>
public class ActivatePersonaTool : BaseTool<ActivatePersonaArguments>
{
    private readonly IPersonaOrchestrator _orchestrator;

    public ActivatePersonaTool(IPersonaOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    public override string Name => "activate_persona";
    
    public override string Description => 
        "Activate or deactivate a specific persona. This controls whether the persona is available for selection and interaction.";

    public override JsonElement InputSchema => CreateSchema<ActivatePersonaArguments>();

    protected override async Task<CallToolResponse> ExecuteInternalAsync(
        ActivatePersonaArguments arguments, 
        CancellationToken cancellationToken)
    {
        try
        {
            var success = await _orchestrator.SetPersonaStatusAsync(arguments.PersonaId, arguments.IsActive);
            
            if (success)
            {
                var status = arguments.IsActive ? "activated" : "deactivated";
                return CreateSuccessResponse($"Persona '{arguments.PersonaId}' has been {status} successfully.");
            }
            else
            {
                return CreateErrorResponse($"Failed to update persona '{arguments.PersonaId}' status. Persona may not exist.");
            }
        }
        catch (Exception ex)
        {
            return CreateErrorResponse($"Error updating persona status: {ex.Message}");
        }
    }
}

public class ActivatePersonaArguments
{
    /// <summary>
    /// The ID of the persona to activate/deactivate (e.g., 'devops-engineer', 'sre-specialist', 'security-engineer', 'engineering-manager')
    /// </summary>
    public string PersonaId { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether to activate (true) or deactivate (false) the persona
    /// </summary>
    public bool IsActive { get; set; }
}