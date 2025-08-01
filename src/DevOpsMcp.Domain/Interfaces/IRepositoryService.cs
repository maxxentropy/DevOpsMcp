using DevOpsMcp.Domain.Entities;

namespace DevOpsMcp.Domain.Interfaces;

public interface IRepositoryService
{
    Task<Repository?> GetByIdAsync(string projectId, string repositoryId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Repository>> GetRepositoriesAsync(string projectId, CancellationToken cancellationToken = default);
    Task<Repository> CreateAsync(string projectId, CreateRepositoryRequest request, CancellationToken cancellationToken = default);
    Task<Repository> UpdateAsync(string projectId, string repositoryId, UpdateRepositoryRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(string projectId, string repositoryId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GitRef>> GetBranchesAsync(string projectId, string repositoryId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GitCommit>> GetCommitsAsync(string projectId, string repositoryId, string? branch = null, int? top = null, CancellationToken cancellationToken = default);
}

public sealed record CreateRepositoryRequest
{
    public required string Name { get; init; }
    public string? ParentRepositoryId { get; init; }
}

public sealed record UpdateRepositoryRequest
{
    public string? DefaultBranch { get; init; }
    public bool? IsDisabled { get; init; }
}

public sealed record GitRef
{
    public required string Name { get; init; }
    public required string ObjectId { get; init; }
    public required GitRefType RefType { get; init; }
}

public sealed record GitCommit
{
    public required string CommitId { get; init; }
    public required string Author { get; init; }
    public required DateTime AuthorDate { get; init; }
    public required string Committer { get; init; }
    public required DateTime CommitterDate { get; init; }
    public required string Comment { get; init; }
    public IReadOnlyList<string> Parents { get; init; } = Array.Empty<string>();
}

public enum GitRefType
{
    Branch,
    Tag,
    Note
}