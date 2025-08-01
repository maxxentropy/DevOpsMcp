namespace DevOpsMcp.Domain.Entities;

public sealed record PullRequest
{
    public required int Id { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string SourceRefName { get; init; }
    public required string TargetRefName { get; init; }
    public required PullRequestStatus Status { get; init; }
    public required string CreatedBy { get; init; }
    public required DateTime CreationDate { get; init; }
    public DateTime? ClosedDate { get; init; }
    public required string RepositoryId { get; init; }
    public string? MergeId { get; init; }
    public required MergeStatus MergeStatus { get; init; }
    public bool IsDraft { get; init; }
    public IReadOnlyList<PullRequestReviewer> Reviewers { get; init; } = Array.Empty<PullRequestReviewer>();
    public IReadOnlyList<string> Labels { get; init; } = Array.Empty<string>();
    public IReadOnlyList<int> WorkItemRefs { get; init; } = Array.Empty<int>();
    public PullRequestCompletionOptions? CompletionOptions { get; init; }
    public string? AutoCompleteSetBy { get; init; }
}

public sealed record PullRequestReviewer
{
    public required string ReviewerId { get; init; }
    public required string DisplayName { get; init; }
    public required int Vote { get; init; }
    public bool IsRequired { get; init; }
    public bool HasDeclined { get; init; }
    public bool IsFlagged { get; init; }
}

public sealed record PullRequestCompletionOptions
{
    public bool DeleteSourceBranch { get; init; }
    public bool SquashMerge { get; init; }
    public MergeStrategy MergeStrategy { get; init; }
    public bool TransitionWorkItems { get; init; }
    public bool AutoComplete { get; init; }
}

public enum PullRequestStatus
{
    NotSet,
    Active,
    Abandoned,
    Completed
}

public enum MergeStatus
{
    NotSet,
    Queued,
    Conflicts,
    Succeeded,
    RejectedByPolicy,
    Failure
}

public enum MergeStrategy
{
    NoFastForward,
    Squash,
    Rebase,
    RebaseMerge
}