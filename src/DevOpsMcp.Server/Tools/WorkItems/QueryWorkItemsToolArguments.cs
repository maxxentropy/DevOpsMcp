using System.ComponentModel;

namespace DevOpsMcp.Server.Tools.WorkItems;

public sealed record QueryWorkItemsToolArguments
{
    [Description("The project ID to query work items from")]
    public required string ProjectId { get; init; }
    
    [Description("WIQL (Work Item Query Language) query. Note: TOP is not supported, use 'limit' parameter instead")]
    public required string Wiql { get; init; }
    
    [Description("Maximum number of work items to return (default: 50, max: 200)")]
    public int? Limit { get; init; } = 50;
    
    [Description("Number of work items to skip for pagination (default: 0)")]
    public int? Skip { get; init; }
    
    [Description("Specific fields to return. If not specified, returns standard fields only")]
    public IReadOnlyList<string>? Fields { get; init; }
    
    [Description("Include work item relations in response (default: false)")]
    public bool IncludeRelations { get; init; }
}