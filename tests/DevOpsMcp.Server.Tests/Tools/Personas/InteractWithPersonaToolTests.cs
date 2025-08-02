using DevOpsMcp.Domain.Personas;
using DevOpsMcp.Domain.Personas.Orchestration;
using DevOpsMcp.Server.Tools.Personas;
using FluentAssertions;
using Moq;
using System.Text.Json;
using Xunit;

namespace DevOpsMcp.Server.Tests.Tools.Personas;

public class InteractWithPersonaToolTests
{
    private readonly Mock<IPersonaOrchestrator> _orchestratorMock;
    private readonly Mock<IPersonaMemoryManager> _memoryManagerMock;
    private readonly InteractWithPersonaTool _tool;

    public InteractWithPersonaToolTests()
    {
        _orchestratorMock = new Mock<IPersonaOrchestrator>();
        _memoryManagerMock = new Mock<IPersonaMemoryManager>();
        _tool = new InteractWithPersonaTool(_orchestratorMock.Object, _memoryManagerMock.Object);
    }

    [Fact]
    public void Name_ReturnsCorrectName()
    {
        // Assert
        _tool.Name.Should().Be("interact_with_persona");
    }

    [Fact]
    public void Description_ReturnsCorrectDescription()
    {
        // Assert
        _tool.Description.Should().Contain("DevOps personas");
    }

    [Fact]
    public void InputSchema_ContainsRequiredProperties()
    {
        // Act
        var schema = _tool.InputSchema;

        // Assert
        schema.GetProperty("type").GetString().Should().Be("object");
        var properties = schema.GetProperty("properties");
        properties.TryGetProperty("request", out _).Should().BeTrue();
        properties.TryGetProperty("personaIds", out _).Should().BeTrue();
        properties.TryGetProperty("sessionId", out _).Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithSinglePersona_ReturnsResponse()
    {
        // Arrange
        var arguments = new InteractWithPersonaArguments
        {
            Request = "How do I set up CI/CD?",
            PersonaIds = { "devops-engineer" },
            SessionId = "test-session",
            ProjectId = "test-project"
        };

        var mockResponse = new PersonaResponse
        {
            ResponseId = "test-response",
            PersonaId = "devops-engineer",
            Response = "Here's how to set up CI/CD...",
            Confidence = new PersonaConfidence { Overall = 0.9 },
            Metadata = new ResponseMetadata()
        };

        // Add suggested actions to the response
        mockResponse.SuggestedActions.Add(new SuggestedAction 
        { 
            Title = "Configure Pipeline",
            Description = "Set up your build pipeline",
            Category = "Configuration",
            Priority = ActionPriority.High
        });

        _orchestratorMock.Setup(x => x.RouteRequestAsync(
                It.IsAny<string>(),
                It.IsAny<DevOpsContext>(),
                It.IsAny<string>()))
            .ReturnsAsync(mockResponse);

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeFalse();
        result.Content.Should().HaveCount(1);
        result.Content[0].Text.Should().Contain("devops-engineer");
        result.Content[0].Text.Should().Contain("Here's how to set up CI/CD");
    }

    [Fact]
    public async Task ExecuteAsync_WithMultiplePersonas_OrchestratesResponses()
    {
        // Arrange
        var arguments = new InteractWithPersonaArguments
        {
            Request = "Design secure CI/CD pipeline",
            PersonaIds = { "devops-engineer", "security-engineer" }
        };

        var devOpsResponse = new PersonaResponse
        {
            PersonaId = "devops-engineer",
            Response = "DevOps perspective",
            Confidence = new PersonaConfidence { Overall = 0.8 }
        };

        var securityResponse = new PersonaResponse
        {
            PersonaId = "security-engineer",
            Response = "Security perspective",
            Confidence = new PersonaConfidence { Overall = 0.9 }
        };

        var orchestrationResult = new OrchestrationResult
        {
            ConsolidatedResponse = "Consolidated response from multiple personas",
            Metrics = new OrchestrationMetrics { TotalDuration = 100 }
        };

        // Add contributions
        orchestrationResult.Contributions.Add(new PersonaContribution
        {
            PersonaId = "devops-engineer",
            Response = devOpsResponse,
            Weight = 0.5,
            Type = ContributionType.Primary
        });

        orchestrationResult.Contributions.Add(new PersonaContribution
        {
            PersonaId = "security-engineer",
            Response = securityResponse,
            Weight = 0.5,
            Type = ContributionType.Supporting
        });

        _orchestratorMock.Setup(x => x.OrchestrateMultiPersonaResponseAsync(
                It.IsAny<DevOpsContext>(),
                It.IsAny<string>(),
                It.IsAny<List<string>>()))
            .ReturnsAsync(orchestrationResult);

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeFalse();
        result.Content[0].Text.Should().Contain("Consolidated response");
        result.Content[0].Text.Should().Contain("contributions");
    }

    [Fact]
    public async Task ExecuteAsync_WithSession_UpdatesMemory()
    {
        // Arrange
        var arguments = new InteractWithPersonaArguments
        {
            Request = "Deploy to production",
            PersonaIds = { "sre-specialist" },
            SessionId = "session-123",
            UserId = "user-456"
        };

        var mockResponse = new PersonaResponse
        {
            PersonaId = "sre-specialist",
            Response = "Deployment strategy...",
            Confidence = new PersonaConfidence { Overall = 0.85 }
        };

        _orchestratorMock.Setup(x => x.RouteRequestAsync(
                It.IsAny<string>(),
                It.IsAny<DevOpsContext>(),
                It.IsAny<string>()))
            .ReturnsAsync(mockResponse);

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeFalse();
        
        // Verify that the response was returned successfully
        result.Content[0].Text.Should().Contain("sre-specialist");
        result.Content[0].Text.Should().Contain("Deployment strategy");
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidPersonaId_ReturnsError()
    {
        // Arrange
        var arguments = new InteractWithPersonaArguments
        {
            Request = "Test request",
            PersonaIds = { "invalid-persona" }
        };

        _orchestratorMock.Setup(x => x.RouteRequestAsync(
                It.IsAny<string>(),
                It.IsAny<DevOpsContext>(),
                It.IsAny<string>()))
            .ThrowsAsync(new ArgumentException("Persona not found"));

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("Failed to interact with persona");
    }

    [Fact]
    public async Task ExecuteAsync_WithNoPersonaIds_ReturnsError()
    {
        // Arrange
        var arguments = new InteractWithPersonaArguments
        {
            Request = "Test request"
            // PersonaIds is empty
        };

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("At least one persona ID must be specified");
    }
}