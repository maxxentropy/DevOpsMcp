using System.Text.RegularExpressions;

namespace DevOpsMcp.Application.Validators;

/// <summary>
/// Validates WIQL queries and provides helpful error messages
/// </summary>
public static partial class WiqlValidator
{
    [GeneratedRegex(@"\bTOP\s+\d+\b", RegexOptions.IgnoreCase)]
    private static partial Regex TopClauseRegex();
    
    [GeneratedRegex(@"\bDISTINCT\b", RegexOptions.IgnoreCase)]
    private static partial Regex DistinctClauseRegex();
    
    [GeneratedRegex(@"\bSELECT\b", RegexOptions.IgnoreCase)]
    private static partial Regex SelectClauseRegex();
    
    [GeneratedRegex(@"\bFROM\s+WorkItems\b", RegexOptions.IgnoreCase)]
    private static partial Regex FromClauseRegex();
    
    [GeneratedRegex(@"\[System\.TeamProject\]\s*=", RegexOptions.IgnoreCase)]
    private static partial Regex TeamProjectFilterRegex();

    public static ValidationResult Validate(string wiql, string? projectId = null)
    {
        if (string.IsNullOrWhiteSpace(wiql))
        {
            return ValidationResult.Error("WIQL query cannot be empty");
        }

        // Check for TOP clause
        if (TopClauseRegex().IsMatch(wiql))
        {
            return ValidationResult.Error(
                "TOP clause is not supported in Azure DevOps WIQL. " +
                "Use the 'limit' parameter instead to control the number of results returned.");
        }

        // Check for DISTINCT clause
        if (DistinctClauseRegex().IsMatch(wiql))
        {
            return ValidationResult.Error(
                "DISTINCT clause is not supported in Azure DevOps WIQL. " +
                "Consider using GROUP BY or post-processing the results.");
        }

        // Check for required SELECT clause
        if (!SelectClauseRegex().IsMatch(wiql))
        {
            return ValidationResult.Error(
                "WIQL query must contain a SELECT clause. " +
                "Example: SELECT [System.Id] FROM WorkItems WHERE [System.WorkItemType] = 'Bug'");
        }

        // Check for required FROM clause
        if (!FromClauseRegex().IsMatch(wiql))
        {
            return ValidationResult.Error(
                "WIQL query must contain 'FROM WorkItems'. " +
                "This is the only valid table in Azure DevOps WIQL.");
        }

        // Warn if no WHERE clause
        if (!wiql.Contains("WHERE", StringComparison.OrdinalIgnoreCase))
        {
            return ValidationResult.Warning(
                "WIQL query has no WHERE clause. This will return ALL work items in the project. " +
                "Consider adding filters like: WHERE [System.WorkItemType] = 'Bug' AND [System.State] = 'Active'");
        }

        // Warn if no team project filter when projectId is provided
        if (projectId != null && !TeamProjectFilterRegex().IsMatch(wiql))
        {
            return ValidationResult.Warning(
                $"WIQL query doesn't filter by team project. " +
                $"Consider adding: AND [System.TeamProject] = '{projectId}' to limit results to the current project.");
        }

        return ValidationResult.Success();
    }

    public static string GetSampleQuery(string workItemType = "Bug")
    {
        return $@"SELECT [System.Id], [System.Title], [System.State] 
FROM WorkItems 
WHERE [System.WorkItemType] = '{workItemType}' 
  AND [System.State] <> 'Closed' 
ORDER BY [System.CreatedDate] DESC";
    }

    public static string TransformTopClause(string wiql, out int? topCount)
    {
        topCount = null;
        var match = TopClauseRegex().Match(wiql);
        
        if (match.Success)
        {
            // Extract the number from TOP clause
            var topMatch = Regex.Match(match.Value, @"\d+");
            if (topMatch.Success && int.TryParse(topMatch.Value, out var count))
            {
                topCount = count;
            }
            
            // Remove the TOP clause from the query
            return wiql.Replace(match.Value, "").Trim();
        }
        
        return wiql;
    }
}

public class ValidationResult
{
    public bool IsValid { get; private init; }
    public bool IsWarning { get; private init; }
    public string? Message { get; private init; }

    private ValidationResult(bool isValid, bool isWarning, string? message)
    {
        IsValid = isValid;
        IsWarning = isWarning;
        Message = message;
    }

    public static ValidationResult Success() => new(true, false, null);
    public static ValidationResult Warning(string message) => new(true, true, message);
    public static ValidationResult Error(string message) => new(false, false, message);
}