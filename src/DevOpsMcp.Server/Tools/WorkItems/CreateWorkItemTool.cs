using DevOpsMcp.Server.Mcp;

namespace DevOpsMcp.Server.Tools.WorkItems;

public sealed class CreateWorkItemTool(IMediator mediator) : BaseTool<CreateWorkItemToolArguments>
{
    public override string Name => "create_work_item";
    public override string Description => "Create a new work item in Azure DevOps";
    public override JsonElement InputSchema => CreateSchema<CreateWorkItemToolArguments>();

    protected override async Task<CallToolResponse> ExecuteInternalAsync(CreateWorkItemToolArguments arguments, CancellationToken cancellationToken)
    {
        var command = new CreateWorkItemCommand
        {
            ProjectId = arguments.ProjectId,
            WorkItemType = arguments.WorkItemType,
            Title = arguments.Title,
            Description = arguments.Description,
            AssignedTo = arguments.AssignedTo,
            AreaPath = arguments.AreaPath,
            IterationPath = arguments.IterationPath,
            Priority = arguments.Priority,
            Severity = arguments.Severity,
            Tags = arguments.Tags,
            AdditionalFields = arguments.AdditionalFields
        };

        var result = await mediator.Send(command, cancellationToken);

        if (result.IsError)
        {
            return CreateErrorResponse($"Failed to create work item: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return CreateJsonResponse(new
        {
            workItem = result.Value,
            message = $"Work item #{result.Value.Id} created successfully"
        });
    }
}