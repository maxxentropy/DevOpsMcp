namespace DevOpsMcp.Domain.Entities;

public sealed record Repository
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string DefaultBranch { get; init; }
    public required long Size { get; init; }
    public required string RemoteUrl { get; init; }
    public required string WebUrl { get; init; }
    public required string ProjectId { get; init; }
    public bool IsDisabled { get; init; }
    public bool IsFork { get; init; }
    public string? ParentRepositoryId { get; init; }
    
    public static Repository Create(
        string id,
        string name,
        string projectId,
        string defaultBranch = "main")
    {
        return new Repository
        {
            Id = id,
            Name = name,
            DefaultBranch = defaultBranch,
            ProjectId = projectId,
            Size = 0,
            RemoteUrl = string.Empty,
            WebUrl = string.Empty
        };
    }
}