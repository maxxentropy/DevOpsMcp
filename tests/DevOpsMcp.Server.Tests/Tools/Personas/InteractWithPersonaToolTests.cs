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
            Confidence = new ResponseConfidence { Overall = 0.9 },
            SuggestedActions = new List<DevOpsAction>(),
            Context = new Dictionary<string, object>(),
            Metadata = new ResponseMetadata()
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

        var orchestrationResult = new OrchestrationResult
        {
            ConsolidatedResponse = "Consolidated response from multiple personas",
            Contributions = new List<PersonaContribution>
            {
                new PersonaContribution
                {
                    PersonaId = "devops-engineer",
                    Response = new PersonaResponse { Response = "DevOps perspective" },
                    Weight = 0.5,
                    Type = ContributionType.Primary
                },
                new PersonaContribution
                {
                    PersonaId = "security-engineer",
                    Response = new PersonaResponse { Response = "Security perspective" },
                    Weight = 0.5,
                    Type = ContributionType.Supporting
                }
            },
            Metrics = new OrchestrationMetrics { TotalDuration = 100 },
            CombinedContext = new Dictionary<string, object>()
        };

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
            Request = "Test request",
            PersonaIds = { "devops-engineer" },
            SessionId = "test-session"
        };

        var mockResponse = new PersonaResponse
        {
            ResponseId = "test-response",
            Response = "Test response",
            Metadata = new ResponseMetadata { IntentClassification = "query" }
        };

        _orchestratorMock.Setup(x => x.RouteRequestAsync(
                It.IsAny<string>(),
                It.IsAny<DevOpsContext>(),
                It.IsAny<string>()))
            .ReturnsAsync(mockResponse);

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        await _tool.ExecuteAsync(jsonArgs);

        // Assert
        _memoryManagerMock.Verify(x => x.StoreConversationContextAsync(
            It.IsAny<string>(),
            It.IsAny<ConversationContext>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithUserContext_BuildsCorrectContext()
    {
        // Arrange
        var arguments = new InteractWithPersonaArguments
        {
            Request = "Test request",
            PersonaIds = { "devops-engineer" },
            UserId = "user123",
            UserName = "John Doe",
            UserRole = "Senior Developer",
            UserExperienceLevel = "Advanced",
            ProjectId = "proj123",
            ProjectName = "Test Project",
            ProjectStage = "Production",
            EnvironmentType = "Production",
            IsProduction = true
        };

        DevOpsContext? capturedContext = null;
        _orchestratorMock.Setup(x => x.RouteRequestAsync(
                It.IsAny<string>(),
                It.IsAny<DevOpsContext>(),
                It.IsAny<string>()))
            .Callback<string, DevOpsContext, string>((_, context, __) => capturedContext = context)
            .ReturnsAsync(new PersonaResponse { Response = "Test" });

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        await _tool.ExecuteAsync(jsonArgs);

        // Assert
        capturedContext.Should().NotBeNull();
        capturedContext!.User.Should().NotBeNull();
        capturedContext.User.Id.Should().Be("user123");
        capturedContext.User.Name.Should().Be("John Doe");
        capturedContext.Project.ProjectId.Should().Be("proj123");
        capturedContext.Environment.IsProduction.Should().BeTrue();
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
            .ThrowsAsync(new ArgumentException("Invalid persona"));

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("Error during persona interaction");
    }
}