namespace DevOpsMcp.Server.Tools.WorkItems;

public sealed class CreateWorkItemToolArguments
{
    public required string ProjectId { get; init; }
    public required string WorkItemType { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public string? AssignedTo { get; init; }
    public required string AreaPath { get; init; }
    public required string IterationPath { get; init; }
    public int? Priority { get; init; }
    public string? Severity { get; init; }
    public List<string>? Tags { get; init; }
    public Dictionary<string, object>? AdditionalFields { get; init; }
}