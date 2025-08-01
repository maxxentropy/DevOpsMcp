namespace DevOpsMcp.Domain.Entities;

public sealed record Project
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string OrganizationUrl { get; init; }
    public required ProjectVisibility Visibility { get; init; }
    public required ProjectState State { get; init; }
    public required DateTime CreatedDate { get; init; }
    public DateTime? LastUpdateTime { get; init; }
    public Dictionary<string, object> Properties { get; init; } = new();
    
    public static Project Create(
        string id,
        string name,
        string description,
        string organizationUrl,
        ProjectVisibility visibility = ProjectVisibility.Private,
        ProjectState state = ProjectState.WellFormed)
    {
        return new Project
        {
            Id = id,
            Name = name,
            Description = description,
            OrganizationUrl = organizationUrl,
            Visibility = visibility,
            State = state,
            CreatedDate = DateTime.UtcNow
        };
    }
}

public enum ProjectVisibility
{
    Private,
    Public,
    Organization
}

public enum ProjectState
{
    New,
    WellFormed,
    CreatePending,
    Deleting,
    Deleted,
    Unchanged
}