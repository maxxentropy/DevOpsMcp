using DevOpsMcp.Domain.Entities;

namespace DevOpsMcp.Domain.Events;

public sealed record BuildCompletedEvent : INotification
{
    public required string ProjectId { get; init; }
    public required int BuildId { get; init; }
    public required string BuildNumber { get; init; }
    public required BuildStatus Status { get; init; }
    public required BuildResult Result { get; init; }
    public required DateTime FinishTime { get; init; }
    public TimeSpan? Duration { get; init; }
}