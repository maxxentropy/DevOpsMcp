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
    
    /// <summary>
    /// Desired output format (plain, json, xml, yaml, table)
    /// </summary>
    public string OutputFormat { get; init; } = "plain";
    
    /// <summary>
    /// Packages to import before execution
    /// </summary>
    public List<string>? ImportedPackages { get; init; }
    
    /// <summary>
    /// Working directory for script execution
    /// </summary>
    public string? WorkingDirectory { get; init; }
    
    /// <summary>
    /// Environment variables to set (as JSON object)
    /// </summary>
    public string? EnvironmentVariablesJson { get; init; }
}