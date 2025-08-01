using DevOpsMcp.Domain.Personas.Orchestration;
using DevOpsMcp.Server.Tools.Personas;
using FluentAssertions;
using Moq;
using System.Text.Json;
using Xunit;

namespace DevOpsMcp.Server.Tests.Tools.Personas;

public class ActivatePersonaToolTests
{
    private readonly Mock<IPersonaOrchestrator> _orchestratorMock;
    private readonly ActivatePersonaTool _tool;

    public ActivatePersonaToolTests()
    {
        _orchestratorMock = new Mock<IPersonaOrchestrator>();
        _tool = new ActivatePersonaTool(_orchestratorMock.Object);
    }

    [Fact]
    public void Name_ReturnsCorrectName()
    {
        // Assert
        _tool.Name.Should().Be("activate_persona");
    }

    [Fact]
    public void Description_ReturnsCorrectDescription()
    {
        // Assert
        _tool.Description.Should().Contain("Activate or deactivate");
    }

    [Fact]
    public async Task ExecuteAsync_WithActivateAction_ActivatesPersona()
    {
        // Arrange
        var arguments = new ActivatePersonaArguments
        {
            PersonaId = "devops-engineer",
            IsActive = true
        };

        _orchestratorMock.Setup(x => x.SetPersonaStatusAsync("devops-engineer", true))
            .ReturnsAsync(true);

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeFalse();
        result.Content[0].Text.Should().Contain("activated");
        result.Content[0].Text.Should().Contain("devops-engineer");
        _orchestratorMock.Verify(x => x.SetPersonaStatusAsync("devops-engineer", true), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithDeactivateAction_DeactivatesPersona()
    {
        // Arrange
        var arguments = new ActivatePersonaArguments
        {
            PersonaId = "security-engineer",
            IsActive = false
        };

        _orchestratorMock.Setup(x => x.SetPersonaStatusAsync("security-engineer", false))
            .ReturnsAsync(true);

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeFalse();
        result.Content[0].Text.Should().Contain("deactivated");
        result.Content[0].Text.Should().Contain("security-engineer");
        _orchestratorMock.Verify(x => x.SetPersonaStatusAsync("security-engineer", false), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentPersona_ReturnsErrorMessage()
    {
        // Arrange
        var arguments = new ActivatePersonaArguments
        {
            PersonaId = "invalid-persona",
            IsActive = true
        };

        _orchestratorMock.Setup(x => x.SetPersonaStatusAsync("invalid-persona", true))
            .ReturnsAsync(false);

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("Failed to update persona");
    }

    [Fact]
    public async Task ExecuteAsync_WhenOperationFails_ReturnsError()
    {
        // Arrange
        var arguments = new ActivatePersonaArguments
        {
            PersonaId = "non-existent-persona",
            IsActive = true
        };

        _orchestratorMock.Setup(x => x.SetPersonaStatusAsync("non-existent-persona", true))
            .ReturnsAsync(false);

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("Failed to update persona status");
    }

    [Fact]
    public async Task ExecuteAsync_WithException_ReturnsError()
    {
        // Arrange
        var arguments = new ActivatePersonaArguments
        {
            PersonaId = "devops-engineer",
            IsActive = true
        };

        _orchestratorMock.Setup(x => x.SetPersonaStatusAsync(It.IsAny<string>(), It.IsAny<bool>()))
            .ThrowsAsync(new Exception("Orchestrator error"));

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("Error updating persona status");
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
        properties.TryGetProperty("isActive", out _).Should().BeTrue();
        
        var required = schema.GetProperty("required");
        required.EnumerateArray().Select(e => e.GetString())
            .Should().Contain("personaId");
    }
}