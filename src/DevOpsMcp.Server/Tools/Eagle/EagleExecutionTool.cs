using DevOpsMcp.Application.Commands.Eagle;
using DevOpsMcp.Server.Mcp;
using MediatR;
using System.ComponentModel;
using System.Text.Json;

namespace DevOpsMcp.Server.Tools.Eagle;

/// <summary>
/// MCP tool for Eagle script execution
/// </summary>
public sealed class EagleExecutionTool(IMediator mediator) : BaseTool<EagleExecutionToolArguments>
{
    public override string Name => "execute_eagle_script";
    
    public override string Description => 
        "Execute an Eagle/Tcl script in a secure sandbox with configurable security policies";
    
    public override JsonElement InputSchema => CreateSchema<EagleExecutionToolArguments>();

    protected override async Task<CallToolResponse> ExecuteInternalAsync(
        EagleExecutionToolArguments arguments, 
        CancellationToken cancellationToken)
    {
        var command = new ExecuteEagleScriptCommand
        {
            Script = arguments.Script,
            VariablesJson = arguments.VariablesJson,
            SecurityLevel = arguments.SecurityLevel ?? "Standard",
            SessionId = arguments.SessionId,
            TimeoutSeconds = arguments.TimeoutSeconds ?? 30
        };

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsError)
        {
            return CreateErrorResponse(
                $"Script execution failed: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        var executionResult = result.Value;
        
        return CreateJsonResponse(new
        {
            executionResult.ExecutionId,
            executionResult.IsSuccess,
            result = executionResult.Result,
            error = executionResult.ErrorMessage,
            executionResult.Duration,
            metrics = new
            {
                executionResult.Metrics.CommandsExecuted,
                compilationTimeMs = executionResult.Metrics.CompilationTime.TotalMilliseconds,
                executionTimeMs = executionResult.Metrics.ExecutionTime.TotalMilliseconds,
                memoryUsageMb = executionResult.Metrics.MemoryUsageBytes / (1024.0 * 1024.0)
            },
            executionResult.ExitCode,
            executionResult.SecurityViolations
        });
    }
}

/// <summary>
/// Arguments for Eagle script execution
/// </summary>
public sealed record EagleExecutionToolArguments
{
    [Description("The Eagle/Tcl script to execute")]
    public required string Script { get; init; }
    
    [Description("JSON object containing variables to set before execution")]
    public string? VariablesJson { get; init; }
    
    [Description("Security level: Minimal, Standard, Elevated, or Maximum")]
    public string? SecurityLevel { get; init; }
    
    [Description("Session ID for stateful execution across multiple calls")]
    public string? SessionId { get; init; }
    
    [Description("Maximum execution time in seconds (default: 30)")]
    public int? TimeoutSeconds { get; init; }
}