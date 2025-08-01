namespace DevOpsMcp.Domain.Events;

public sealed record WorkItemCreatedEvent : INotification
{
    public required string ProjectId { get; init; }
    public required int WorkItemId { get; init; }
    public required string WorkItemType { get; init; }
    public required string Title { get; init; }
    public required string CreatedBy { get; init; }
    public required DateTime CreatedDate { get; init; }
}