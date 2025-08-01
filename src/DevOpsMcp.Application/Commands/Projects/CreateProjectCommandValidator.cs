namespace DevOpsMcp.Application.Commands.Projects;

public sealed class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Project name is required")
            .Length(1, 64).WithMessage("Project name must be between 1 and 64 characters")
            .Matches(@"^[a-zA-Z0-9\s\-_]+$").WithMessage("Project name contains invalid characters");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Project description is required")
            .MaximumLength(256).WithMessage("Project description must not exceed 256 characters");

        RuleFor(x => x.OrganizationUrl)
            .NotEmpty().WithMessage("Organization URL is required");

        RuleFor(x => x.Visibility)
            .Must(BeValidVisibility).WithMessage("Invalid project visibility");
    }

    private bool BeValidVisibility(string visibility)
    {
        return Enum.TryParse<ProjectVisibility>(visibility, true, out _);
    }
}