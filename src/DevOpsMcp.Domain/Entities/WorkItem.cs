namespace DevOpsMcp.Domain.Entities;

public sealed record WorkItem
{
    public required int Id { get; init; }
    public required string WorkItemType { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required WorkItemState State { get; init; }
    public string? AssignedTo { get; init; }
    public required string AreaPath { get; init; }
    public required string IterationPath { get; init; }
    public required DateTime CreatedDate { get; init; }
    public required string CreatedBy { get; init; }
    public DateTime? ChangedDate { get; init; }
    public string? ChangedBy { get; init; }
    public int? Priority { get; init; }
    public string? Severity { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();
    public IReadOnlyList<WorkItemRelation> Relations { get; init; } = Array.Empty<WorkItemRelation>();
    public Dictionary<string, object> Fields { get; init; } = new();
    
    public static WorkItem Create(
        string workItemType,
        string title,
        string areaPath,
        string iterationPath,
        string createdBy)
    {
        return new WorkItem
        {
            Id = 0,
            WorkItemType = workItemType,
            Title = title,
            State = WorkItemState.New,
            AreaPath = areaPath,
            IterationPath = iterationPath,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = createdBy
        };
    }
}

public sealed record WorkItemRelation
{
    public required string RelationType { get; init; }
    public required string TargetUrl { get; init; }
    public required int TargetId { get; init; }
    public Dictionary<string, object> Attributes { get; init; } = new();
}

public enum WorkItemState
{
    New,
    Active,
    Resolved,
    Closed,
    Removed
}