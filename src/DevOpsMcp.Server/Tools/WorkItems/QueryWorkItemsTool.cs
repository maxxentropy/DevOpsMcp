using DevOpsMcp.Server.Mcp;

namespace DevOpsMcp.Server.Tools.WorkItems;

public sealed class QueryWorkItemsTool(IMediator mediator) : BaseTool<QueryWorkItemsToolArguments>
{
    public override string Name => "query_work_items";
    public override string Description => "Query work items using WIQL (Work Item Query Language)";
    public override JsonElement InputSchema => CreateSchema<QueryWorkItemsToolArguments>();

    protected override async Task<CallToolResponse> ExecuteInternalAsync(QueryWorkItemsToolArguments arguments, CancellationToken cancellationToken)
    {
        var query = new QueryWorkItemsQuery
        {
            ProjectId = arguments.ProjectId,
            Wiql = arguments.Wiql
        };

        var result = await mediator.Send(query, cancellationToken);

        if (result.IsError)
        {
            return CreateErrorResponse($"Failed to query work items: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return CreateJsonResponse(new
        {
            workItems = result.Value,
            count = result.Value.Count,
            query = arguments.Wiql
        });
    }
}