using DevOpsMcp.Domain.Entities;

namespace DevOpsMcp.Domain.Interfaces;

public interface IPullRequestService
{
    Task<PullRequest?> GetByIdAsync(string projectId, string repositoryId, int pullRequestId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PullRequest>> GetPullRequestsAsync(string projectId, string repositoryId, PullRequestFilter? filter = null, CancellationToken cancellationToken = default);
    Task<PullRequest> CreateAsync(string projectId, string repositoryId, CreatePullRequestRequest request, CancellationToken cancellationToken = default);
    Task<PullRequest> UpdateAsync(string projectId, string repositoryId, int pullRequestId, UpdatePullRequestRequest request, CancellationToken cancellationToken = default);
    Task<PullRequest> CompletePullRequestAsync(string projectId, string repositoryId, int pullRequestId, PullRequestCompletionOptions options, CancellationToken cancellationToken = default);
    Task<PullRequest> AbandonPullRequestAsync(string projectId, string repositoryId, int pullRequestId, CancellationToken cancellationToken = default);
    Task<PullRequest> ReactivatePullRequestAsync(string projectId, string repositoryId, int pullRequestId, CancellationToken cancellationToken = default);
    Task<PullRequest> AddReviewerAsync(string projectId, string repositoryId, int pullRequestId, string reviewerId, bool isRequired = false, CancellationToken cancellationToken = default);
    Task<PullRequest> VoteAsync(string projectId, string repositoryId, int pullRequestId, int vote, CancellationToken cancellationToken = default);
    Task<PullRequestComment> AddCommentAsync(string projectId, string repositoryId, int pullRequestId, string content, string? parentCommentId = null, CancellationToken cancellationToken = default);
}

public sealed record PullRequestFilter
{
    public PullRequestStatus? Status { get; init; }
    public string? CreatorId { get; init; }
    public string? ReviewerId { get; init; }
    public string? SourceBranch { get; init; }
    public string? TargetBranch { get; init; }
    public int? Top { get; init; }
}

public sealed record CreatePullRequestRequest
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required string SourceRefName { get; init; }
    public required string TargetRefName { get; init; }
    public bool IsDraft { get; init; }
    public IReadOnlyList<string>? ReviewerIds { get; init; }
    public IReadOnlyList<string>? Labels { get; init; }
    public IReadOnlyList<int>? WorkItemIds { get; init; }
}

public sealed record UpdatePullRequestRequest
{
    public string? Title { get; init; }
    public string? Description { get; init; }
    public bool? IsDraft { get; init; }
    public PullRequestCompletionOptions? AutoCompleteOptions { get; init; }
}

public sealed record PullRequestComment
{
    public required int Id { get; init; }
    public required string Content { get; init; }
    public required string Author { get; init; }
    public required DateTime PublishedDate { get; init; }
    public int? ParentCommentId { get; init; }
    public CommentThreadStatus ThreadStatus { get; init; }
}

public enum CommentThreadStatus
{
    Unknown,
    Active,
    Fixed,
    WontFix,
    Closed,
    ByDesign,
    Pending
}