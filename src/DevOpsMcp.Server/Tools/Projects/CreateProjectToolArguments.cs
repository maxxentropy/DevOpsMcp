namespace DevOpsMcp.Server.Tools.Projects;

public sealed class CreateProjectToolArguments
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public string Visibility { get; init; } = "Private";
    public Dictionary<string, object>? Properties { get; init; }
}