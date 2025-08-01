using DevOpsMcp.Server.Mcp;

namespace DevOpsMcp.Server.Tools.WorkItems;

public sealed class CreateWorkItemTool : BaseTool<CreateWorkItemTool.Arguments>
{
    private readonly IMediator _mediator;

    public class Arguments
    {
        public required string ProjectId { get; set; }
        public required string WorkItemType { get; set; }
        public required string Title { get; set; }
        public string? Description { get; set; }
        public string? AssignedTo { get; set; }
        public required string AreaPath { get; set; }
        public required string IterationPath { get; set; }
        public int? Priority { get; set; }
        public string? Severity { get; set; }
        public List<string>? Tags { get; set; }
        public Dictionary<string, object>? AdditionalFields { get; set; }
    }

    public override string Name => "create_work_item";
    public override string Description => "Create a new work item in Azure DevOps";
    public override JsonElement InputSchema => CreateSchema<Arguments>();

    public CreateWorkItemTool(IMediator mediator)
    {
        _mediator = mediator;
    }

    protected override async Task<CallToolResponse> ExecuteInternalAsync(Arguments arguments, CancellationToken cancellationToken)
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

        var result = await _mediator.Send(command, cancellationToken);

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