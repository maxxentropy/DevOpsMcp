using DevOpsMcp.Domain.Personas;
using DevOpsMcp.Domain.Personas.Adaptation;
using DevOpsMcp.Server.Tools.Personas;
using FluentAssertions;
using Moq;
using System.Text.Json;
using Xunit;

namespace DevOpsMcp.Server.Tests.Tools.Personas;

public class ConfigurePersonaBehaviorToolTests
{
    private readonly Mock<IPersonaBehaviorAdapter> _behaviorAdapterMock;
    private readonly ConfigurePersonaBehaviorTool _tool;

    public ConfigurePersonaBehaviorToolTests()
    {
        _behaviorAdapterMock = new Mock<IPersonaBehaviorAdapter>();
        var serviceProviderMock = new Mock<IServiceProvider>();
        _tool = new ConfigurePersonaBehaviorTool(_behaviorAdapterMock.Object, serviceProviderMock.Object);
    }

    [Fact]
    public void Name_ReturnsCorrectName()
    {
        // Assert
        _tool.Name.Should().Be("configure_persona_behavior");
    }

    [Fact]
    public void Description_ReturnsCorrectDescription()
    {
        // Assert
        _tool.Description.Should().Contain("Configure persona behavior");
    }

    [Fact]
    public async Task ExecuteAsync_WithUserPreferences_AdaptsBehavior()
    {
        // Arrange
        var arguments = new ConfigurePersonaBehaviorArguments
        {
            PersonaId = "devops-engineer",
            CommunicationStyle = "concise",
            TechnicalLevel = "intermediate",
            ResponseLength = "standard"
        };

        var userPreferences = new UserPreferences
        {
            UserId = "user123",
            CommunicationPreference = PreferredCommunicationStyle.Concise,
            PreferredTechnicalDepth = TechnicalDepth.Intermediate,
            PreferredResponseLength = ResponseLength.Standard
        };

        var currentConfig = new PersonaConfiguration
        {
            CommunicationStyle = CommunicationStyle.Collaborative,
            TechnicalDepth = TechnicalDepth.Intermediate,
            ResponseFormat = ResponseFormat.Standard
        };

        var adaptedConfig = new PersonaConfiguration
        {
            CommunicationStyle = CommunicationStyle.Concise,
            TechnicalDepth = TechnicalDepth.Intermediate,
            ResponseFormat = ResponseFormat.Standard
        };

        _behaviorAdapterMock.Setup(x => x.AdaptConfigurationAsync(
                "devops-engineer",
                It.IsAny<PersonaConfiguration>(),
                It.IsAny<UserPreferences>(),
                It.IsAny<ProjectContext>()))
            .ReturnsAsync(adaptedConfig);

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeFalse();
        
        var responseJson = result.Content[0].Text;
        responseJson.Should().Contain("Successfully configured");
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidPersonaId_ReturnsError()
    {
        // Arrange
        var arguments = new ConfigurePersonaBehaviorArguments
        {
            PersonaId = "invalid-persona",
            CommunicationStyle = "concise"
        };

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("not found");
    }

    [Fact]
    public async Task ExecuteAsync_WithException_ReturnsError()
    {
        // Arrange
        var arguments = new ConfigurePersonaBehaviorArguments
        {
            PersonaId = "devops-engineer",
            CommunicationStyle = "concise"
        };

        _behaviorAdapterMock.Setup(x => x.AdaptConfigurationAsync(
                It.IsAny<string>(),
                It.IsAny<PersonaConfiguration>(),
                It.IsAny<UserPreferences>(),
                It.IsAny<ProjectContext>()))
            .ThrowsAsync(new InvalidOperationException("Test error"));

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("Failed to configure persona behavior");
    }

    [Fact]
    public void InputSchema_ContainsRequiredProperties()
    {
        // Act
        var schema = _tool.InputSchema;

        // Assert
        schema.GetProperty("type").GetString().Should().Be("object");
        var properties = schema.GetProperty("properties");
        properties.TryGetProperty("personaId", out _).Should().BeTrue();
        properties.TryGetProperty("communicationStyle", out _).Should().BeTrue();
        properties.TryGetProperty("responseLength", out _).Should().BeTrue();
        properties.TryGetProperty("technicalLevel", out _).Should().BeTrue();
    }
}