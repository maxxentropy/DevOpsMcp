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
        _tool = new ConfigurePersonaBehaviorTool(_behaviorAdapterMock.Object);
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
            ConfigurationType = "user_preferences",
            UserId = "user123",
            PreferredCommunicationStyle = "concise",
            TechnicalLevel = "intermediate",
            ResponseDetailLevel = "balanced"
        };

        var adaptation = new BehaviorAdaptation
        {
            PersonaId = "devops-engineer",
            AdaptationType = AdaptationType.UserPreference,
            Confidence = 0.9,
            Adjustments = new Dictionary<string, object>
            {
                ["communication_style"] = "concise",
                ["technical_depth"] = "intermediate"
            }
        };

        _behaviorAdapterMock.Setup(x => x.AdaptToUserPreferencesAsync(
                "devops-engineer",
                It.IsAny<UserPreferences>()))
            .ReturnsAsync(adaptation);

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeFalse();
        
        var responseJson = result.Content[0].Text;
        responseJson.Should().Contain("Successfully configured");
        responseJson.Should().Contain("UserPreference");
        responseJson.Should().Contain("0.9");
    }

    [Fact]
    public async Task ExecuteAsync_WithProjectContext_AdaptsToProject()
    {
        // Arrange
        var arguments = new ConfigurePersonaBehaviorArguments
        {
            PersonaId = "security-engineer",
            ConfigurationType = "project_context",
            ProjectId = "high-security-project",
            ProjectStage = "Production",
            ComplianceLevel = "high",
            TechnologyStack = new List<string> { "Kubernetes", "AWS" }
        };

        var adaptation = new BehaviorAdaptation
        {
            PersonaId = "security-engineer",
            AdaptationType = AdaptationType.ProjectContext,
            Confidence = 0.95,
            Adjustments = new Dictionary<string, object>
            {
                ["security_focus"] = "maximum",
                ["compliance_rigor"] = "high"
            }
        };

        _behaviorAdapterMock.Setup(x => x.AdaptToProjectContextAsync(
                "security-engineer",
                It.IsAny<ProjectContext>()))
            .ReturnsAsync(adaptation);

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeFalse();
        result.Content[0].Text.Should().Contain("ProjectContext");
        result.Content[0].Text.Should().Contain("security_focus");
    }

    [Fact]
    public async Task ExecuteAsync_WithTeamDynamics_AdaptsToTeam()
    {
        // Arrange
        var arguments = new ConfigurePersonaBehaviorArguments
        {
            PersonaId = "engineering-manager",
            ConfigurationType = "team_dynamics",
            TeamSize = 20,
            TeamMaturityLevel = "intermediate",
            TeamPreferences = new Dictionary<string, object>
            {
                ["communication_frequency"] = "daily",
                ["documentation_style"] = "detailed"
            }
        };

        var adaptation = new BehaviorAdaptation
        {
            PersonaId = "engineering-manager",
            AdaptationType = AdaptationType.TeamDynamics,
            Confidence = 0.85
        };

        _behaviorAdapterMock.Setup(x => x.AdaptToTeamDynamicsAsync(
                "engineering-manager",
                It.IsAny<TeamDynamics>()))
            .ReturnsAsync(adaptation);

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeFalse();
        result.Content[0].Text.Should().Contain("TeamDynamics");
    }

    [Fact]
    public async Task ExecuteAsync_WithHistoricalLearning_AppliesLearning()
    {
        // Arrange
        var arguments = new ConfigurePersonaBehaviorArguments
        {
            PersonaId = "sre-specialist",
            ConfigurationType = "historical_learning",
            SessionIds = new List<string> { "session1", "session2" },
            LearningWeight = 0.8
        };

        var adaptation = new BehaviorAdaptation
        {
            PersonaId = "sre-specialist",
            AdaptationType = AdaptationType.HistoricalLearning,
            Confidence = 0.88,
            Adjustments = new Dictionary<string, object>
            {
                ["learned_patterns"] = 15,
                ["adaptation_strength"] = 0.8
            }
        };

        _behaviorAdapterMock.Setup(x => x.ApplyHistoricalLearningAsync(
                "sre-specialist",
                It.IsAny<List<string>>(),
                0.8))
            .ReturnsAsync(adaptation);

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeFalse();
        result.Content[0].Text.Should().Contain("HistoricalLearning");
        result.Content[0].Text.Should().Contain("learned_patterns");
    }

    [Fact]
    public async Task ExecuteAsync_WithResetConfiguration_ResetsToDefault()
    {
        // Arrange
        var arguments = new ConfigurePersonaBehaviorArguments
        {
            PersonaId = "devops-engineer",
            ConfigurationType = "reset"
        };

        _behaviorAdapterMock.Setup(x => x.ResetToDefaultBehaviorAsync("devops-engineer"))
            .ReturnsAsync(true);

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeFalse();
        result.Content[0].Text.Should().Contain("reset to default");
        result.Content[0].Text.Should().Contain("devops-engineer");
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidConfigurationType_ReturnsError()
    {
        // Arrange
        var arguments = new ConfigurePersonaBehaviorArguments
        {
            PersonaId = "test",
            ConfigurationType = "invalid_type"
        };

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("Invalid configuration type");
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingRequiredData_ReturnsError()
    {
        // Arrange
        var arguments = new ConfigurePersonaBehaviorArguments
        {
            PersonaId = "test",
            ConfigurationType = "user_preferences"
            // Missing UserId for user_preferences
        };

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("User ID is required");
    }

    [Fact]
    public async Task ExecuteAsync_WithException_ReturnsError()
    {
        // Arrange
        var arguments = new ConfigurePersonaBehaviorArguments
        {
            PersonaId = "test",
            ConfigurationType = "reset"
        };

        _behaviorAdapterMock.Setup(x => x.ResetToDefaultBehaviorAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Adapter error"));

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("Error configuring persona behavior");
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
        properties.TryGetProperty("configurationType", out _).Should().BeTrue();
        properties.TryGetProperty("userId", out _).Should().BeTrue();
        properties.TryGetProperty("preferredCommunicationStyle", out _).Should().BeTrue();
        
        var required = schema.GetProperty("required");
        required.EnumerateArray().Select(e => e.GetString())
            .Should().BeEquivalentTo(new[] { "personaId", "configurationType" });
    }

    [Fact]
    public async Task ExecuteAsync_WithViewConfiguration_ReturnsCurrentConfig()
    {
        // Arrange
        var arguments = new ConfigurePersonaBehaviorArguments
        {
            PersonaId = "devops-engineer",
            ConfigurationType = "view"
        };

        var currentConfig = new BehaviorConfiguration
        {
            PersonaId = "devops-engineer",
            CommunicationStyle = CommunicationStyle.Balanced,
            TechnicalDepth = TechnicalDepth.Intermediate,
            ResponseDetailLevel = DetailLevel.Balanced,
            LastUpdated = DateTime.UtcNow.AddHours(-2)
        };

        _behaviorAdapterMock.Setup(x => x.GetCurrentConfigurationAsync("devops-engineer"))
            .ReturnsAsync(currentConfig);

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeFalse();
        result.Content[0].Text.Should().Contain("Current configuration");
        result.Content[0].Text.Should().Contain("Balanced");
        result.Content[0].Text.Should().Contain("Intermediate");
    }
}