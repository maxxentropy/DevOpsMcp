namespace DevOpsMcp.Domain.Entities;

public sealed record Build
{
    public required int Id { get; init; }
    public required string BuildNumber { get; init; }
    public required BuildStatus Status { get; init; }
    public required BuildResult Result { get; init; }
    public required DateTime QueueTime { get; init; }
    public DateTime? StartTime { get; init; }
    public DateTime? FinishTime { get; init; }
    public required string SourceBranch { get; init; }
    public required string SourceVersion { get; init; }
    public required BuildDefinitionReference Definition { get; init; }
    public required string RequestedFor { get; init; }
    public required string RequestedBy { get; init; }
    public required BuildReason Reason { get; init; }
    public IReadOnlyDictionary<string, string> Parameters { get; init; } = new Dictionary<string, string>();
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    
    public TimeSpan? Duration => StartTime.HasValue && FinishTime.HasValue 
        ? FinishTime.Value - StartTime.Value 
        : null;
}

public sealed record BuildDefinitionReference
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string Path { get; init; }
    public required BuildDefinitionType Type { get; init; }
}

public enum BuildStatus
{
    None,
    InProgress,
    Completed,
    Cancelling,
    Postponed,
    NotStarted,
    Paused
}

public enum BuildResult
{
    None,
    Succeeded,
    PartiallySucceeded,
    Failed,
    Canceled
}

public enum BuildReason
{
    None,
    Manual,
    IndividualCI,
    BatchedCI,
    Schedule,
    UserCreated,
    PullRequest,
    BuildCompletion,
    ResourceTrigger
}

public enum BuildDefinitionType
{
    Build,
    Xaml,
    Yaml
}