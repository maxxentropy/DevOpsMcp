namespace DevOpsMcp.Server.Tools.WorkItems;

public sealed class QueryWorkItemsToolArguments
{
    public required string ProjectId { get; init; }
    public required string Wiql { get; init; }
}