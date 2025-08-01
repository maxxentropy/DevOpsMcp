using DevOpsMcp.Domain.Personas.Orchestration;
using DevOpsMcp.Server.Mcp;
using System.Text.Json;

namespace DevOpsMcp.Server.Tools.Personas;

/// <summary>
/// Tool for retrieving the status and health of personas
/// </summary>
public class GetPersonaStatusTool : BaseTool<GetPersonaStatusArguments>
{
    private readonly IPersonaOrchestrator _orchestrator;

    public GetPersonaStatusTool(IPersonaOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    public override string Name => "get_persona_status";
    
    public override string Description => 
        "Get the current status, health, and performance metrics of all active personas.";

    public override JsonElement InputSchema => CreateSchema<GetPersonaStatusArguments>();

    protected override async Task<CallToolResponse> ExecuteInternalAsync(
        GetPersonaStatusArguments arguments, 
        CancellationToken cancellationToken)
    {
        try
        {
            var activePersonas = await _orchestrator.GetActivePersonasAsync();
            
            var statusReport = new
            {
                timestamp = DateTime.UtcNow,
                totalActive = activePersonas.Count,
                personas = activePersonas.Select(p => new
                {
                    personaId = p.PersonaId,
                    isActive = p.IsActive,
                    health = new
                    {
                        status = p.Health.Status.ToString(),
                        successRate = p.Health.SuccessRate,
                        errorCount = p.Health.ErrorCount,
                        lastError = p.Health.LastError,
                        averageSatisfaction = p.Health.AverageSatisfaction
                    },
                    performance = new
                    {
                        currentLoad = p.CurrentLoad,
                        averageResponseTime = p.AverageResponseTime,
                        lastActivated = p.LastActivated
                    }
                }),
                summary = new
                {
                    healthyCount = activePersonas.Count(p => p.Health.Status == HealthStatus.Healthy),
                    degradedCount = activePersonas.Count(p => p.Health.Status == HealthStatus.Degraded),
                    unhealthyCount = activePersonas.Count(p => p.Health.Status == HealthStatus.Unhealthy),
                    averageLoad = activePersonas.Any() ? activePersonas.Average(p => p.CurrentLoad) : 0,
                    averageResponseTime = activePersonas.Any() ? activePersonas.Average(p => p.AverageResponseTime) : 0
                }
            };

            return CreateJsonResponse(statusReport);
        }
        catch (Exception ex)
        {
            return CreateErrorResponse($"Error retrieving persona status: {ex.Message}");
        }
    }
}

public class GetPersonaStatusArguments
{
    // This tool doesn't require any arguments
}