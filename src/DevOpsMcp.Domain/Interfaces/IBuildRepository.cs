using DevOpsMcp.Domain.Entities;

namespace DevOpsMcp.Domain.Interfaces;

public interface IBuildRepository
{
    Task<Build?> GetByIdAsync(string projectId, int buildId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Build>> GetBuildsAsync(string projectId, BuildFilter? filter = null, CancellationToken cancellationToken = default);
    Task<Build> QueueBuildAsync(string projectId, QueueBuildRequest request, CancellationToken cancellationToken = default);
    Task<Build> UpdateBuildAsync(string projectId, int buildId, BuildUpdateRequest request, CancellationToken cancellationToken = default);
    Task<Build> CancelBuildAsync(string projectId, int buildId, CancellationToken cancellationToken = default);
    Task<string> GetBuildLogsAsync(string projectId, int buildId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BuildArtifact>> GetBuildArtifactsAsync(string projectId, int buildId, CancellationToken cancellationToken = default);
}

public sealed record BuildFilter
{
    public int? DefinitionId { get; init; }
    public BuildStatus? Status { get; init; }
    public BuildResult? Result { get; init; }
    public string? BranchName { get; init; }
    public DateTime? MinTime { get; init; }
    public DateTime? MaxTime { get; init; }
    public int? Top { get; init; }
}

public sealed record QueueBuildRequest
{
    public required int DefinitionId { get; init; }
    public string? SourceBranch { get; init; }
    public Dictionary<string, string>? Parameters { get; init; }
    public string? Reason { get; init; }
}

public sealed record BuildUpdateRequest
{
    public bool? KeepForever { get; init; }
    public bool? RetainIndefinitely { get; init; }
}

public sealed record BuildArtifact
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string Resource { get; init; }
}