using System.ComponentModel;
using System.Text.Json;
using DevOpsMcp.Domain.Interfaces;
using DevOpsMcp.Server.Mcp;
using Microsoft.Extensions.Logging;

namespace DevOpsMcp.Server.Tools.Eagle;

/// <summary>
/// Tool for querying Eagle script execution history
/// </summary>
public sealed class EagleHistoryTool(
    IEagleScriptExecutor eagleExecutor,
    ILogger<EagleHistoryTool> logger) : BaseTool<EagleHistoryToolArguments>
{
    public override string Name => "eagle_history";
    
    public override string Description => 
        "Query Eagle script execution history with optional filtering by session ID";
    
    public override JsonElement InputSchema => CreateSchema<EagleHistoryToolArguments>();

    protected override async Task<CallToolResponse> ExecuteInternalAsync(
        EagleHistoryToolArguments arguments,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Querying Eagle execution history with limit {Limit}", arguments.Limit);

            var history = await eagleExecutor.GetExecutionHistoryAsync(
                arguments.SessionId,
                arguments.Limit,
                cancellationToken);

            if (history.Count == 0)
            {
                return CreateSuccessResponse("No execution history found");
            }

            // Format history based on requested detail level
            var formattedHistory = arguments.Detailed
                ? FormatDetailedHistory(history)
                : FormatSummaryHistory(history);

            return CreateSuccessResponse(formattedHistory);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to query Eagle execution history");
            return CreateErrorResponse($"Failed to query execution history: {ex.Message}");
        }
    }

    private string FormatSummaryHistory(IReadOnlyList<Domain.Eagle.EagleExecutionResult> history)
    {
        var lines = new List<string>
        {
            $"Eagle Execution History ({history.Count} entries):",
            "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
        };

        foreach (var execution in history)
        {
            var status = execution.IsSuccess ? "✓" : "✗";
            var duration = execution.Metrics?.ExecutionTime.TotalMilliseconds ?? 0;
            var timestamp = execution.StartTimeUtc.ToString("yyyy-MM-dd HH:mm:ss");
            
            lines.Add($"{status} [{execution.ExecutionId[..8]}] {timestamp} - {duration:F0}ms");
            
            if (!execution.IsSuccess && !string.IsNullOrEmpty(execution.ErrorMessage))
            {
                lines.Add($"  Error: {execution.ErrorMessage}");
            }
        }

        return string.Join("\n", lines);
    }

    private string FormatDetailedHistory(IReadOnlyList<Domain.Eagle.EagleExecutionResult> history)
    {
        var lines = new List<string>
        {
            $"Eagle Execution History - Detailed ({history.Count} entries):",
            "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
        };

        foreach (var execution in history)
        {
            lines.Add($"\nExecution ID: {execution.ExecutionId}");
            lines.Add($"Status: {(execution.IsSuccess ? "Success" : "Failed")}");
            lines.Add($"Start Time: {execution.StartTimeUtc:yyyy-MM-dd HH:mm:ss.fff} UTC");
            lines.Add($"End Time: {execution.EndTimeUtc:yyyy-MM-dd HH:mm:ss.fff} UTC");
            lines.Add($"Exit Code: {execution.ExitCode}");
            
            if (execution.Metrics != null)
            {
                lines.Add("\nMetrics:");
                lines.Add($"  Compilation Time: {execution.Metrics.CompilationTime.TotalMilliseconds:F2}ms");
                lines.Add($"  Execution Time: {execution.Metrics.ExecutionTime.TotalMilliseconds:F2}ms");
                lines.Add($"  Commands Executed: {execution.Metrics.CommandsExecuted}");
                lines.Add($"  Memory Usage: {execution.Metrics.MemoryUsageBytes / 1024.0 / 1024.0:F2} MB");
                lines.Add($"  Security Checks: {execution.Metrics.SecurityChecksPerformed}");
            }
            
            if (!string.IsNullOrEmpty(execution.Result))
            {
                lines.Add($"\nResult: {TruncateText(execution.Result, 200)}");
            }
            
            if (!string.IsNullOrEmpty(execution.ErrorMessage))
            {
                lines.Add($"\nError: {execution.ErrorMessage}");
            }
            
            lines.Add("─────────────────────────────────────────────");
        }

        return string.Join("\n", lines);
    }

    private static string TruncateText(string text, int maxLength)
    {
        if (text.Length <= maxLength)
            return text;
        
        return text[..(maxLength - 3)] + "...";
    }

}

public sealed record EagleHistoryToolArguments
{
    [Description("Optional session ID to filter history")]
    public string? SessionId { get; init; }

    [Description("Maximum number of entries to return")]
    public int Limit { get; init; } = 10;

    [Description("Include detailed metrics and results")]
    public bool Detailed { get; init; } = false;
}