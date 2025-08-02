using DevOpsMcp.Domain.Eagle;
using ErrorOr;
using MediatR;

namespace DevOpsMcp.Application.Commands.Eagle;

/// <summary>
/// Command to execute an Eagle script
/// </summary>
public sealed record ExecuteEagleScriptCommand : IRequest<ErrorOr<EagleExecutionResult>>
{
    /// <summary>
    /// The Eagle/Tcl script to execute
    /// </summary>
    public required string Script { get; init; }
    
    /// <summary>
    /// Variables to set before execution (as JSON)
    /// </summary>
    public string? VariablesJson { get; init; }
    
    /// <summary>
    /// Security level for execution
    /// </summary>
    public string SecurityLevel { get; init; } = "Standard";
    
    /// <summary>
    /// Session ID for stateful execution
    /// </summary>
    public string? SessionId { get; init; }
    
    /// <summary>
    /// Timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; init; } = 30;
}