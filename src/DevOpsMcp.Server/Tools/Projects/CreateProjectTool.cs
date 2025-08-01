using DevOpsMcp.Server.Mcp;
using DevOpsMcp.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace DevOpsMcp.Server.Tools.Projects;

public sealed class CreateProjectTool : BaseTool<CreateProjectTool.Arguments>
{
    private readonly IMediator _mediator;
    private readonly IOptions<AzureDevOpsOptions> _options;

    public class Arguments
    {
        public required string Name { get; set; }
        public required string Description { get; set; }
        public string Visibility { get; set; } = "Private";
        public Dictionary<string, object>? Properties { get; set; }
    }

    public override string Name => "create_project";
    public override string Description => "Create a new project in Azure DevOps";
    public override JsonElement InputSchema => CreateSchema<Arguments>();

    public CreateProjectTool(IMediator mediator, IOptions<AzureDevOpsOptions> options)
    {
        _mediator = mediator;
        _options = options;
    }

    protected override async Task<CallToolResponse> ExecuteInternalAsync(Arguments arguments, CancellationToken cancellationToken)
    {
        var command = new CreateProjectCommand
        {
            Name = arguments.Name,
            Description = arguments.Description,
            OrganizationUrl = _options.Value.OrganizationUrl,
            Visibility = arguments.Visibility,
            Properties = arguments.Properties
        };

        var result = await _mediator.Send(command, cancellationToken);

        if (result.IsError)
        {
            return CreateErrorResponse($"Failed to create project: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return CreateJsonResponse(new
        {
            project = result.Value,
            message = $"Project '{arguments.Name}' created successfully"
        });
    }
}