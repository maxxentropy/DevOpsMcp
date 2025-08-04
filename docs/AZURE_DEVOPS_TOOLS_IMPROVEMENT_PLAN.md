# Azure DevOps Tools Improvement Plan

## Problem Summary
Current Azure DevOps tools return excessive data causing:
- Token limit exceeded errors
- Slow response times
- Poor user experience
- Cryptic error messages

## Improvement Strategy

### 1. Query Result Management

#### A. Add Pagination Support
```csharp
public sealed class QueryWorkItemsToolArguments
{
    public required string ProjectId { get; init; }
    public required string Wiql { get; init; }
    
    // New pagination parameters
    public int? Limit { get; init; } = 50;  // Default 50 items
    public int? Skip { get; init; } = 0;    // For pagination
    public string[]? Fields { get; init; }  // Specific fields to return
    public bool? IncludeRelations { get; init; } = false; // Don't expand by default
}
```

#### B. Implement Smart WIQL Enhancement
- Automatically inject result limiting if not present
- Parse WIQL to detect and warn about unsupported clauses (like TOP)
- Add ORDER BY [System.Id] DESC if no ordering specified

### 2. Response Size Control

#### A. Field Selection Strategy
```csharp
// Default minimal fields
private static readonly string[] DefaultFields = new[]
{
    "System.Id",
    "System.Title", 
    "System.WorkItemType",
    "System.State",
    "System.AssignedTo",
    "System.CreatedDate",
    "System.ChangedDate"
};

// Full field expansion only when requested
public enum FieldExpansion
{
    Minimal,    // ID, Title, Type, State only
    Default,    // Common fields (7-10 fields)
    Extended,   // All standard fields
    Full        // Everything including custom fields
}
```

#### B. Result Summary Mode
```csharp
public class WorkItemSummaryDto
{
    public int Id { get; init; }
    public string Title { get; init; }
    public string WorkItemType { get; init; }
    public string State { get; init; }
    public string AssignedTo { get; init; }
}
```

### 3. Common Query Shortcuts

#### A. New Specialized Tools
```csharp
// get_recent_work_items - Simple tool for common use case
public sealed class GetRecentWorkItemsTool
{
    // Parameters: ProjectId, WorkItemType?, State?, Count=10
    // Auto-generates: SELECT [System.Id] FROM WorkItems 
    //                 WHERE [System.TeamProject] = @project
    //                 ORDER BY [System.ChangedDate] DESC
}

// get_my_work_items - Gets items assigned to current user
public sealed class GetMyWorkItemsTool
{
    // Parameters: ProjectId, State?, Count=25
    // Uses context to get current user
}

// get_work_item_by_id - Optimized single item fetch
public sealed class GetWorkItemByIdTool
{
    // Parameters: Id, Fields?
    // Direct fetch without WIQL
}
```

### 4. Error Handling Improvements

#### A. WIQL Validation
```csharp
public class WiqlValidator
{
    private static readonly string[] UnsupportedClauses = { "TOP", "DISTINCT" };
    
    public ValidationResult Validate(string wiql)
    {
        // Check for unsupported clauses
        // Validate field references
        // Ensure proper syntax
        // Return helpful error messages
    }
}
```

#### B. Better Error Messages
```csharp
// Instead of: "VS403437: The query references a FROM clause"
// Return: "WIQL Error: TOP clause is not supported. Use the 'limit' parameter instead."

// Instead of returning 10,000 items and failing
// Return: "Query would return 10,000 items. Limited to 50. Use 'skip' parameter for pagination."
```

### 5. Implementation Priority

#### Phase 1 - Critical (Immediate)
1. Add `limit` parameter to QueryWorkItemsTool (default: 50)
2. Remove `WorkItemExpand.All` - use specific fields
3. Add result count to response metadata

#### Phase 2 - High Priority (Week 1)
1. Implement field selection
2. Add skip/pagination support
3. Create GetRecentWorkItemsTool

#### Phase 3 - Medium Priority (Week 2)
1. WIQL validator with helpful errors
2. GetMyWorkItemsTool
3. GetWorkItemByIdTool
4. Summary mode option

#### Phase 4 - Enhancement (Week 3)
1. Query templates
2. Smart defaults based on work item type
3. Caching for repeated queries

## Example Usage After Improvements

### Before (Problematic)
```json
{
  "tool": "query_work_items",
  "arguments": {
    "projectId": "MyProject",
    "wiql": "SELECT [System.Id] FROM WorkItems WHERE [System.WorkItemType] = 'Bug'"
  }
}
// Returns: ALL bugs with ALL fields - could be thousands
```

### After (Improved)
```json
{
  "tool": "query_work_items",
  "arguments": {
    "projectId": "MyProject",
    "wiql": "SELECT [System.Id] FROM WorkItems WHERE [System.WorkItemType] = 'Bug'",
    "limit": 25,
    "skip": 0,
    "fields": ["System.Id", "System.Title", "System.State", "System.AssignedTo"]
  }
}
// Returns: First 25 bugs with only requested fields
```

### Shortcut Alternative
```json
{
  "tool": "get_recent_work_items",
  "arguments": {
    "projectId": "MyProject",
    "workItemType": "Bug",
    "count": 10
  }
}
// Returns: 10 most recently updated bugs with standard fields
```

## Success Metrics
- No more token limit errors
- Average response time < 2 seconds
- 90% of queries use shortcuts instead of raw WIQL
- Clear, actionable error messages
- Predictable response sizes