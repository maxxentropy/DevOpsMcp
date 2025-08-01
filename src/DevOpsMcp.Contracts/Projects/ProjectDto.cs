namespace DevOpsMcp.Contracts.Projects;

public sealed record ProjectDto
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string OrganizationUrl { get; init; }
    public required string Visibility { get; init; }
    public required string State { get; init; }
    public required DateTime CreatedDate { get; init; }
    public DateTime? LastUpdateTime { get; init; }
    public Dictionary<string, object> Properties { get; init; } = new();
}