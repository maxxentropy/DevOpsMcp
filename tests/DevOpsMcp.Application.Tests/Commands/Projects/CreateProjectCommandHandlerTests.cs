namespace DevOpsMcp.Application.Tests.Commands.Projects;

public class CreateProjectCommandHandlerTests
{
    private readonly Mock<IProjectRepository> _projectRepositoryMock;
    private readonly Mock<ILogger<CreateProjectCommandHandler>> _loggerMock;
    private readonly CreateProjectCommandHandler _handler;

    public CreateProjectCommandHandlerTests()
    {
        _projectRepositoryMock = new Mock<IProjectRepository>();
        _loggerMock = new Mock<ILogger<CreateProjectCommandHandler>>();
        _handler = new CreateProjectCommandHandler(
            _projectRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesProjectSuccessfully()
    {
        // Arrange
        var command = new CreateProjectCommand
        {
            Name = "Test Project",
            Description = "Test Description",
            OrganizationUrl = "https://dev.azure.com/testorg",
            Visibility = "Private"
        };

        var createdProject = Project.Create(
            Guid.NewGuid().ToString(),
            command.Name,
            command.Description,
            command.OrganizationUrl,
            ProjectVisibility.Private);

        _projectRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdProject);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.Name.Should().Be(command.Name);
        result.Value.Description.Should().Be(command.Description);
        result.Value.Visibility.Should().Be("Private");

        _projectRepositoryMock.Verify(
            x => x.CreateAsync(It.Is<Project>(p => 
                p.Name == command.Name && 
                p.Description == command.Description), 
                It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidOrganizationUrl_ReturnsError()
    {
        // Arrange
        var command = new CreateProjectCommand
        {
            Name = "Test Project",
            Description = "Test Description",
            OrganizationUrl = "invalid-url",
            Visibility = "Private"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("OrganizationUrl.Invalid");

        _projectRepositoryMock.Verify(
            x => x.CreateAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), 
            Times.Never);
    }

    [Fact]
    public async Task Handle_InvalidVisibility_ReturnsError()
    {
        // Arrange
        var command = new CreateProjectCommand
        {
            Name = "Test Project",
            Description = "Test Description",
            OrganizationUrl = "https://dev.azure.com/testorg",
            Visibility = "InvalidVisibility"
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Project.InvalidVisibility");
    }

    [Fact]
    public async Task Handle_WithCustomProperties_IncludesPropertiesInProject()
    {
        // Arrange
        var command = new CreateProjectCommand
        {
            Name = "Test Project",
            Description = "Test Description",
            OrganizationUrl = "https://dev.azure.com/testorg",
            Visibility = "Private",
            Properties = new Dictionary<string, object>
            {
                ["customField1"] = "value1",
                ["customField2"] = 123
            }
        };

        Project? capturedProject = null;
        _projectRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Callback<Project, CancellationToken>((p, ct) => capturedProject = p)
            .ReturnsAsync((Project p, CancellationToken ct) => p);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsError.Should().BeFalse();
        capturedProject.Should().NotBeNull();
        capturedProject!.Properties.Should().ContainKey("customField1");
        capturedProject.Properties["customField1"].Should().Be("value1");
        capturedProject.Properties.Should().ContainKey("customField2");
        capturedProject.Properties["customField2"].Should().Be(123);
    }

    [Fact]
    public async Task Handle_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        var command = new CreateProjectCommand
        {
            Name = "Test Project",
            Description = "Test Description",
            OrganizationUrl = "https://dev.azure.com/testorg",
            Visibility = "Private"
        };

        _projectRepositoryMock
            .Setup(x => x.CreateAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Repository error"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, CancellationToken.None));
    }
}