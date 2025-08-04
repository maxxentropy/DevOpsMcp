using System;

namespace DevOpsMcp.Domain.Eagle;

/// <summary>
/// Represents an entry in the execution history
/// </summary>
public class ExecutionHistoryEntry
{
    public string ExecutionId { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
    public string Script { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public bool Success { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan ExecutionTime { get; set; }
    public long MemoryUsageBytes { get; set; }
}