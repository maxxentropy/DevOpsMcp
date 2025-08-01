using DevOpsMcp.Domain.Personas;
using DevOpsMcp.Domain.Personas.Orchestration;
using DevOpsMcp.Server.Tools.Personas;
using FluentAssertions;
using Moq;
using System.Text.Json;
using Xunit;

namespace DevOpsMcp.Server.Tests.Tools.Personas;

public class SelectPersonaToolTests
{
    private readonly Mock<IPersonaOrchestrator> _orchestratorMock;
    private readonly SelectPersonaTool _tool;

    public SelectPersonaToolTests()
    {
        _orchestratorMock = new Mock<IPersonaOrchestrator>();
        _tool = new SelectPersonaTool(_orchestratorMock.Object);
    }

    [Fact]
    public void Name_ReturnsCorrectName()
    {
        // Assert
        _tool.Name.Should().Be("select_persona");
    }

    [Fact]
    public async Task ExecuteAsync_WithBestMatchMode_SelectsCorrectPersona()
    {
        // Arrange
        var arguments = new SelectPersonaArguments
        {
            Request = "Set up monitoring for microservices",
            SelectionMode = "best_match",
            MinimumConfidence = 0.7,
            ProjectId = "test-project"
        };

        var selectionResult = new PersonaSelectionResult
        {
            PrimaryPersonaId = "sre-specialist",
            Confidence = 0.85,
            SelectionReason = "Best match with score 0.85"
        };
        
        // Populate scores using reflection or a different approach since it has private setter
        selectionResult.PersonaScores["sre-specialist"] = 0.85;
        selectionResult.PersonaScores["devops-engineer"] = 0.70;
        selectionResult.PersonaScores["security-engineer"] = 0.40;

        _orchestratorMock.Setup(x => x.SelectPersonaAsync(
                It.IsAny<DevOpsContext>(),
                It.IsAny<string>(),
                It.IsAny<PersonaSelectionCriteria>()))
            .ReturnsAsync(selectionResult);

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeFalse();
        
        var responseJson = result.Content[0].Text;
        responseJson.Should().Contain("sre-specialist");
        responseJson.Should().Contain("0.85");
        responseJson.Should().Contain("Best match");
    }

    [Fact]
    public async Task ExecuteAsync_WithSpecializationMode_UsesPreferredSpecialization()
    {
        // Arrange
        var arguments = new SelectPersonaArguments
        {
            Request = "Security audit needed",
            SelectionMode = "specialization",
            PreferredSpecialization = "security"
        };

        PersonaSelectionCriteria? capturedCriteria = null;
        _orchestratorMock.Setup(x => x.SelectPersonaAsync(
                It.IsAny<DevOpsContext>(),
                It.IsAny<string>(),
                It.IsAny<PersonaSelectionCriteria>()))
            .Callback<DevOpsContext, string, PersonaSelectionCriteria>((_, __, criteria) => capturedCriteria = criteria)
            .ReturnsAsync(new PersonaSelectionResult
            {
                PrimaryPersonaId = "security-engineer",
                Confidence = 0.9,
                SelectionReason = "Specialization match: Security"
            });

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        await _tool.ExecuteAsync(jsonArgs);

        // Assert
        capturedCriteria.Should().NotBeNull();
        capturedCriteria!.SelectionMode.Should().Be(PersonaSelectionMode.SpecializationBased);
        capturedCriteria.PreferredSpecializations.Should().Contain(DevOpsSpecialization.Security);
    }

    [Fact]
    public async Task ExecuteAsync_WithMultiplePersonasAllowed_ReturnsSecondaryPersonas()
    {
        // Arrange
        var arguments = new SelectPersonaArguments
        {
            Request = "Complex infrastructure task",
            AllowMultiple = true,
            MaxPersonaCount = 3
        };

        var selectionResult = new PersonaSelectionResult
        {
            PrimaryPersonaId = "devops-engineer",
            Confidence = 0.8,
            SecondaryPersonaIds = { "sre-specialist", "security-engineer" },
            Confidence = 0.8
        };
        selectionResult.PersonaScores["devops-engineer"] = 0.8;
        selectionResult.PersonaScores["sre-specialist"] = 0.75;
        selectionResult.PersonaScores["security-engineer"] = 0.65;

        _orchestratorMock.Setup(x => x.SelectPersonaAsync(
                It.IsAny<DevOpsContext>(),
                It.IsAny<string>(),
                It.IsAny<PersonaSelectionCriteria>()))
            .ReturnsAsync(selectionResult);

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        var responseJson = result.Content[0].Text;
        responseJson.Should().Contain("secondaryPersonas");
        responseJson.Should().Contain("sre-specialist");
        responseJson.Should().Contain("security-engineer");
    }

    [Theory]
    [InlineData("round_robin", PersonaSelectionMode.RoundRobin)]
    [InlineData("load_balanced", PersonaSelectionMode.LoadBalanced)]
    [InlineData("context_aware", PersonaSelectionMode.ContextAware)]
    public async Task ExecuteAsync_ParsesSelectionModeCorrectly(string modeString, PersonaSelectionMode expectedMode)
    {
        // Arrange
        var arguments = new SelectPersonaArguments
        {
            Request = "Test request",
            SelectionMode = modeString
        };

        PersonaSelectionCriteria? capturedCriteria = null;
        _orchestratorMock.Setup(x => x.SelectPersonaAsync(
                It.IsAny<DevOpsContext>(),
                It.IsAny<string>(),
                It.IsAny<PersonaSelectionCriteria>()))
            .Callback<DevOpsContext, string, PersonaSelectionCriteria>((_, __, criteria) => capturedCriteria = criteria)
            .ReturnsAsync(new PersonaSelectionResult { PrimaryPersonaId = "test" });

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        await _tool.ExecuteAsync(jsonArgs);

        // Assert
        capturedCriteria!.SelectionMode.Should().Be(expectedMode);
    }

    [Fact]
    public async Task ExecuteAsync_WithProductionContext_SetsContextCorrectly()
    {
        // Arrange
        var arguments = new SelectPersonaArguments
        {
            Request = "Deploy to production",
            ProjectStage = "Production",
            EnvironmentType = "Production",
            IsProduction = true
        };

        DevOpsContext? capturedContext = null;
        _orchestratorMock.Setup(x => x.SelectPersonaAsync(
                It.IsAny<DevOpsContext>(),
                It.IsAny<string>(),
                It.IsAny<PersonaSelectionCriteria>()))
            .Callback<DevOpsContext, string, PersonaSelectionCriteria>((context, _, __) => capturedContext = context)
            .ReturnsAsync(new PersonaSelectionResult { PrimaryPersonaId = "sre-specialist" });

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        await _tool.ExecuteAsync(jsonArgs);

        // Assert
        capturedContext.Should().NotBeNull();
        capturedContext!.Environment.IsProduction.Should().BeTrue();
        capturedContext.Project.Stage.Should().Be("Production");
    }

    [Fact]
    public async Task ExecuteAsync_WithError_ReturnsErrorResponse()
    {
        // Arrange
        var arguments = new SelectPersonaArguments
        {
            Request = "Test request"
        };

        _orchestratorMock.Setup(x => x.SelectPersonaAsync(
                It.IsAny<DevOpsContext>(),
                It.IsAny<string>(),
                It.IsAny<PersonaSelectionCriteria>()))
            .ThrowsAsync(new Exception("Selection failed"));

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("Error selecting persona");
    }
}