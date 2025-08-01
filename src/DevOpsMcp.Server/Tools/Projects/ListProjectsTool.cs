using DevOpsMcp.Server.Mcp;

namespace DevOpsMcp.Server.Tools.Projects;

public sealed class ListProjectsTool : BaseTool<ListProjectsTool.Arguments>
{
    private readonly IMediator _mediator;

    public class Arguments
    {
        // No arguments needed for listing all projects
    }

    public override string Name => "list_projects";
    public override string Description => "Get all accessible projects in the Azure DevOps organization";
    public override JsonElement InputSchema => CreateSchema<Arguments>();

    public ListProjectsTool(IMediator mediator)
    {
        _mediator = mediator;
    }

    protected override async Task<CallToolResponse> ExecuteInternalAsync(Arguments arguments, CancellationToken cancellationToken)
    {
        var query = new GetProjectsQuery();
        var result = await _mediator.Send(query, cancellationToken);

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