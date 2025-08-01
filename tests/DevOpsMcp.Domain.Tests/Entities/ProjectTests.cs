namespace DevOpsMcp.Domain.Tests.Entities;

public class ProjectTests
{
    private readonly Faker _faker = new();

    [Fact]
    public void Create_ValidParameters_CreatesProject()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var name = _faker.Company.CompanyName();
        var description = _faker.Lorem.Sentence();
        var organizationUrl = "https://dev.azure.com/testorg";

        // Act
        var project = Project.Create(id, name, description, organizationUrl);

        // Assert
        project.Should().NotBeNull();
        project.Id.Should().Be(id);
        project.Name.Should().Be(name);
        project.Description.Should().Be(description);
        project.OrganizationUrl.Should().Be(organizationUrl);
        project.Visibility.Should().Be(ProjectVisibility.Private);
        project.State.Should().Be(ProjectState.WellFormed);
        project.CreatedDate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        project.LastUpdateTime.Should().BeNull();
        project.Properties.Should().BeEmpty();
    }

    [Theory]
    [InlineData(ProjectVisibility.Public)]
    [InlineData(ProjectVisibility.Organization)]
    [InlineData(ProjectVisibility.Private)]
    public void Create_WithVisibility_SetsVisibility(ProjectVisibility visibility)
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var name = _faker.Company.CompanyName();
        var description = _faker.Lorem.Sentence();
        var organizationUrl = "https://dev.azure.com/testorg";

        // Act
        var project = Project.Create(id, name, description, organizationUrl, visibility);

        // Assert
        project.Visibility.Should().Be(visibility);
    }

    [Theory]
    [InlineData(ProjectState.New)]
    [InlineData(ProjectState.CreatePending)]
    [InlineData(ProjectState.Deleting)]
    public void Create_WithState_SetsState(ProjectState state)
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var name = _faker.Company.CompanyName();
        var description = _faker.Lorem.Sentence();
        var organizationUrl = "https://dev.azure.com/testorg";

        // Act
        var project = Project.Create(id, name, description, organizationUrl, ProjectVisibility.Private, state);

        // Assert
        project.State.Should().Be(state);
    }

    [Fact]
    public void RecordEquality_SameValues_AreEqual()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();
        var name = _faker.Company.CompanyName();
        var description = _faker.Lorem.Sentence();
        var organizationUrl = "https://dev.azure.com/testorg";
        var createdDate = DateTime.UtcNow;

        var project1 = new Project
        {
            Id = id,
            Name = name,
            Description = description,
            OrganizationUrl = organizationUrl,
            Visibility = ProjectVisibility.Private,
            State = ProjectState.WellFormed,
            CreatedDate = createdDate
        };

        var project2 = new Project
        {
            Id = id,
            Name = name,
            Description = description,
            OrganizationUrl = organizationUrl,
            Visibility = ProjectVisibility.Private,
            State = ProjectState.WellFormed,
            CreatedDate = createdDate
        };

        // Act & Assert
        project1.Should().Be(project2);
        (project1 == project2).Should().BeTrue();
    }

    [Fact]
    public void RecordEquality_DifferentValues_AreNotEqual()
    {
        // Arrange
        var project1 = Project.Create(
            Guid.NewGuid().ToString(),
            _faker.Company.CompanyName(),
            _faker.Lorem.Sentence(),
            "https://dev.azure.com/testorg");

        var project2 = Project.Create(
            Guid.NewGuid().ToString(),
            _faker.Company.CompanyName(),
            _faker.Lorem.Sentence(),
            "https://dev.azure.com/testorg");

        // Act & Assert
        project1.Should().NotBe(project2);
        (project1 == project2).Should().BeFalse();
    }
}