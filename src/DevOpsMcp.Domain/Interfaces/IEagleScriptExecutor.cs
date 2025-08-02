using DevOpsMcp.Domain.Eagle;

namespace DevOpsMcp.Domain.Interfaces;

/// <summary>
/// Interface for Eagle script execution
/// </summary>
public interface IEagleScriptExecutor
{
    /// <summary>
    /// Execute an Eagle script with the given context
    /// </summary>
    Task<EagleExecutionResult> ExecuteAsync(
        DevOpsMcp.Domain.Eagle.ExecutionContext context, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Validate an Eagle script without executing it
    /// </summary>
    Task<ErrorOr<ValidationResult>> ValidateScriptAsync(
        string script, 
        EagleSecurityPolicy policy, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get the capabilities of the Eagle interpreter
    /// </summary>
    Task<EagleCapabilities> GetCapabilitiesAsync(
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Cancel a running execution
    /// </summary>
    Task<ErrorOr<Success>> CancelExecutionAsync(
        string executionId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get execution history for a session
    /// </summary>
    Task<IReadOnlyList<EagleExecutionResult>> GetExecutionHistoryAsync(
        string? sessionId = null,
        int limit = 10,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of script validation
/// </summary>
public sealed record ValidationResult
{
    public required bool IsValid { get; init; }
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Warnings { get; init; } = Array.Empty<string>();
}

/// <summary>
/// Eagle interpreter capabilities
/// </summary>
public sealed record EagleCapabilities
{
    public required string Version { get; init; }
    public required IReadOnlyList<string> AvailableCommands { get; init; }
    public required IReadOnlyList<string> LoadedPackages { get; init; }
    public required bool SupportsClrIntegration { get; init; }
    public required bool SupportsTcl { get; init; }
}