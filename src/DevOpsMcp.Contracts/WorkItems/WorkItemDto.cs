using System;
using System.Collections.Generic;

namespace DevOpsMcp.Contracts.WorkItems;

public sealed record WorkItemDto
{
    public required int Id { get; init; }
    public required string WorkItemType { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required string State { get; init; }
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
    public IReadOnlyList<WorkItemRelationDto> Relations { get; init; } = Array.Empty<WorkItemRelationDto>();
    public Dictionary<string, object> Fields { get; init; } = new();
}

public sealed record WorkItemRelationDto
{
    public required string RelationType { get; init; }
    public required string TargetUrl { get; init; }
    public required int TargetId { get; init; }
    public Dictionary<string, object> Attributes { get; init; } = new();
}