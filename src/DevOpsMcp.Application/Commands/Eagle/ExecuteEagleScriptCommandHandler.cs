using System.Text.Json;
using DevOpsMcp.Domain.Eagle;
using DevOpsMcp.Domain.Interfaces;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DevOpsMcp.Application.Commands.Eagle;

/// <summary>
/// Handler for executing Eagle scripts
/// </summary>
public sealed class ExecuteEagleScriptCommandHandler : IRequestHandler<ExecuteEagleScriptCommand, ErrorOr<EagleExecutionResult>>
{
    private readonly IEagleScriptExecutor _executor;
    private readonly ILogger<ExecuteEagleScriptCommandHandler> _logger;

    public ExecuteEagleScriptCommandHandler(
        IEagleScriptExecutor executor,
        ILogger<ExecuteEagleScriptCommandHandler> logger)
    {
        _executor = executor;
        _logger = logger;
    }

    public async Task<ErrorOr<EagleExecutionResult>> Handle(
        ExecuteEagleScriptCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Parse variables if provided
            var variables = new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(request.VariablesJson))
            {
                try
                {
                    var parsed = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(request.VariablesJson);
                    if (parsed != null)
                    {
                        foreach (var kvp in parsed)
                        {
                            variables[kvp.Key] = kvp.Value.ValueKind switch
                            {
                                JsonValueKind.String => kvp.Value.GetString()!,
                                JsonValueKind.Number => kvp.Value.GetDouble(),
                                JsonValueKind.True => true,
                                JsonValueKind.False => false,
                                _ => kvp.Value.ToString()
                            };
                        }
                    }
                }
                catch (JsonException ex)
                {
                    return Error.Validation($"Invalid variables JSON: {ex.Message}");
                }
            }

            // Select security policy based on level
            var securityPolicy = request.SecurityLevel.ToLowerInvariant() switch
            {
                "minimal" => EagleSecurityPolicy.Minimal,
                "standard" => EagleSecurityPolicy.Standard,
                _ => EagleSecurityPolicy.Standard
            };

            // Create execution context
            var context = new DevOpsMcp.Domain.Eagle.ExecutionContext
            {
                CorrelationId = Guid.NewGuid(),
                SessionId = request.SessionId,
                Script = request.Script,
                Variables = variables,
                SecurityPolicy = securityPolicy,
                Timeout = TimeSpan.FromSeconds(request.TimeoutSeconds)
            };

            // Execute script
            var result = await _executor.ExecuteAsync(context, cancellationToken);
            
            _logger.LogInformation(
                "Eagle script executed with ID {ExecutionId}, Success: {IsSuccess}", 
                result.ExecutionId, 
                result.IsSuccess);
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute Eagle script");
            return Error.Failure($"Script execution failed: {ex.Message}");
        }
    }
}