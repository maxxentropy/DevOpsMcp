using DevOpsMcp.Server.Mcp;
using DevOpsMcp.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace DevOpsMcp.Server.Tools.Projects;

public sealed class CreateProjectTool(IMediator mediator, IOptions<AzureDevOpsOptions> options)
    : BaseTool<CreateProjectToolArguments>
{
    public override string Name => "create_project";
    public override string Description => "Create a new project in Azure DevOps";
    public override JsonElement InputSchema => CreateSchema<CreateProjectToolArguments>();

    protected override async Task<CallToolResponse> ExecuteInternalAsync(CreateProjectToolArguments arguments, CancellationToken cancellationToken)
    {
        var command = new CreateProjectCommand
        {
            Name = arguments.Name,
            Description = arguments.Description,
            OrganizationUrl = options.Value.OrganizationUrl,
            Visibility = arguments.Visibility,
            Properties = arguments.Properties
        };

        var result = await mediator.Send(command, cancellationToken);

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