using DevOpsMcp.Server.Mcp;

namespace DevOpsMcp.Server.Tools.Projects;

public sealed class ListProjectsTool(IMediator mediator) : BaseTool<ListProjectsToolArguments>
{
    public override string Name => "list_projects";
    public override string Description => "Get all accessible projects in the Azure DevOps organization";
    public override JsonElement InputSchema => CreateSchema<ListProjectsToolArguments>();

    protected override async Task<CallToolResponse> ExecuteInternalAsync(ListProjectsToolArguments arguments, CancellationToken cancellationToken)
    {
        var query = new GetProjectsQuery();
        var result = await mediator.Send(query, cancellationToken);

        if (result.IsError)
        {
            return CreateErrorResponse($"Failed to get projects: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return CreateJsonResponse(new
        {
            projects = result.Value,
            count = result.Value.Count
        });
    }
}