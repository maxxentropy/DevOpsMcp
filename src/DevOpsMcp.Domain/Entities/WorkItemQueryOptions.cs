namespace DevOpsMcp.Domain.Entities;

/// <summary>
/// Options for querying work items with pagination and field selection
/// </summary>
public sealed class WorkItemQueryOptions
{
    /// <summary>
    /// Maximum number of work items to return
    /// </summary>
    public int Limit { get; init; } = 50;
    
    /// <summary>
    /// Number of work items to skip (for pagination)
    /// </summary>
    public int Skip { get; init; }
    
    /// <summary>
    /// Specific fields to retrieve. If null, returns default fields.
    /// </summary>
    public IReadOnlyList<string>? Fields { get; init; }
    
    /// <summary>
    /// Whether to include work item relations
    /// </summary>
    public bool IncludeRelations { get; init; }
    
    /// <summary>
    /// Default fields returned when Fields is not specified
    /// </summary>
    public static readonly IReadOnlyList<string> DefaultFields = new[]
    {
        "System.Id",
        "System.Title",
        "System.WorkItemType", 
        "System.State",
        "System.AssignedTo",
        "System.CreatedDate",
        "System.ChangedDate",
        "System.AreaPath",
        "System.IterationPath",
        "Microsoft.VSTS.Common.Priority"
    };
}