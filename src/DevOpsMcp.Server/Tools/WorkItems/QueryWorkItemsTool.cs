using System.Text.Json;
using MediatR;
using DevOpsMcp.Application.Queries.WorkItems;
using DevOpsMcp.Application.Validators;
using DevOpsMcp.Server.Mcp;

namespace DevOpsMcp.Server.Tools.WorkItems;

public sealed class QueryWorkItemsTool(IMediator mediator) : BaseTool<QueryWorkItemsToolArguments>
{
    public override string Name => "query_work_items";
    public override string Description => @"Query work items using WIQL (Work Item Query Language). 
Returns paginated results with configurable field selection.
Example WIQL: SELECT [System.Id] FROM WorkItems WHERE [System.WorkItemType] = 'Bug' AND [System.State] = 'Active'
Note: Use 'limit' parameter instead of TOP clause. Default limit is 50 items.";
    public override JsonElement InputSchema => CreateSchema<QueryWorkItemsToolArguments>();

    protected override async Task<CallToolResponse> ExecuteInternalAsync(QueryWorkItemsToolArguments arguments, CancellationToken cancellationToken)
    {
        // Pre-validate WIQL to provide immediate feedback
        var wiql = arguments.Wiql;
        var validation = WiqlValidator.Validate(wiql, arguments.ProjectId);
        
        // Check if query has TOP clause and auto-convert
        if (wiql.Contains("TOP", StringComparison.OrdinalIgnoreCase))
        {
            var transformedWiql = WiqlValidator.TransformTopClause(wiql, out var topCount);
            if (topCount.HasValue && !arguments.Limit.HasValue)
            {
                // Use the TOP value as limit if no explicit limit was provided
                arguments = arguments with { Limit = topCount.Value };
                wiql = transformedWiql;
            }
        }
        
        // Validate and enforce limits
        var limit = Math.Min(arguments.Limit ?? 50, 200); // Max 200 items
        var skip = Math.Max(arguments.Skip ?? 0, 0); // Ensure non-negative
        
        var query = new QueryWorkItemsQuery
        {
            ProjectId = arguments.ProjectId,
            Wiql = wiql,
            Limit = limit,
            Skip = skip,
            Fields = arguments.Fields,
            IncludeRelations = arguments.IncludeRelations
        };

        var result = await mediator.Send(query, cancellationToken);

        if (result.IsError)
        {
            // Enhance error messages for common issues
            var errors = result.Errors.Select(e => 
            {
                if (e.Code == "Wiql.Invalid" || e.Code == "Wiql.TopNotSupported" || e.Code == "Wiql.SyntaxError")
                {
                    return e.Description;
                }
                return $"{e.Code}: {e.Description}";
            });
            
            return CreateErrorResponse(string.Join(" ", errors));
        }

        var response = new
        {
            workItems = result.Value,
            count = result.Value.Count,
            query = arguments.Wiql,
            pagination = new
            {
                limit,
                skip,
                hasMore = result.Value.Count == limit // Indicates there might be more results
            }
        };
        
        // Add validation warning if present
        if (validation.IsWarning)
        {
            return CreateJsonResponse(new
            {
                response.workItems,
                response.count,
                response.query,
                response.pagination,
                warning = validation.Message
            });
        }
        
        return CreateJsonResponse(response);
    }
}