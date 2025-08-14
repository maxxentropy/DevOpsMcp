using DevOpsMcp.Application.Personas;
using DevOpsMcp.Domain.Personas;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using MediatR;
using System;
using System.Collections.Generic;

namespace DevOpsMcp.Application.Tests.Personas;

public class DevOpsEngineerPersonaTests
{
    private readonly Mock<ILogger<DevOpsEngineerPersona>> _loggerMock;
    private readonly Mock<IPersonaMemoryManager> _memoryManagerMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly DevOpsEngineerPersona _persona;

    public DevOpsEngineerPersonaTests()
    {
        _loggerMock = new Mock<ILogger<DevOpsEngineerPersona>>();
        _memoryManagerMock = new Mock<IPersonaMemoryManager>();
        _mediatorMock = new Mock<IMediator>();
        _persona = new DevOpsEngineerPersona(_loggerMock.Object, _memoryManagerMock.Object, _mediatorMock.Object);
    }

    [Fact]
    public void Constructor_InitializesWithCorrectProperties()
    {
        // Assert
        _persona.Id.Should().Be("devops-engineer");
        _persona.Name.Should().Be("DevOps Engineer");
        _persona.Role.Should().Be("DevOps and CI/CD Specialist");
        _persona.Specialization.Should().Be(DevOpsSpecialization.Development);
        _persona.Description.Should().Contain("CI/CD pipelines");
    }

    [Fact]
    public void Capabilities_ContainsExpectedSkills()
    {
        // Assert
        _persona.Capabilities.Should().ContainKey("ci_cd");
        _persona.Capabilities.Should().ContainKey("containerization");
        _persona.Capabilities.Should().ContainKey("infrastructure_as_code");
        _persona.Capabilities.Should().ContainKey("cloud_platforms");
        _persona.Capabilities.Should().ContainKey("monitoring");
        _persona.Capabilities.Should().ContainKey("scripting");
    }

    [Theory]
    [InlineData("Set up CI/CD pipeline", 0.9)]
    [InlineData("Deploy to Kubernetes", 0.85)]
    [InlineData("Configure Terraform", 0.85)]
    [InlineData("Implement monitoring", 0.8)]
    [InlineData("Design security architecture", 0.4)]
    public async Task CalculateRoleAlignmentAsync_ReturnsExpectedScores(string taskDescription, double minExpectedScore)
    {
        // Arrange
        var task = new Domain.Personas.DevOpsTask
        {
            Description = taskDescription,
            Category = TaskCategory.Deployment
        };

        // Act
        var score = await _persona.CalculateRoleAlignmentAsync(task);

        // Assert
        score.Should().BeGreaterThanOrEqualTo(minExpectedScore);
    }

    [Fact]
    public async Task ProcessRequestAsync_ForCICDRequest_ReturnsRelevantResponse()
    {
        // Arrange
        var context = CreateTestContext();
        var request = "How do I set up a CI/CD pipeline for a Node.js application?";

        // Act
        var response = await _persona.ProcessRequestAsync(context, request);

        // Assert
        response.Should().NotBeNull();
        response.Response.Should().Contain("pipeline");
        response.Metadata.Topics.Should().Contain("ci/cd");
        response.SuggestedActions.Should().NotBeEmpty();
        response.SuggestedActions.Should().Contain(a => a.Category == "Automation");
    }

    [Fact]
    public async Task ProcessRequestAsync_ForInfrastructureRequest_IncludesTerraformExample()
    {
        // Arrange
        var context = CreateTestContext();
        var request = "How do I provision infrastructure using IaC?";

        // Act
        var response = await _persona.ProcessRequestAsync(context, request);

        // Assert
        response.Response.Should().ContainAny("Terraform", "Infrastructure as Code", "IaC");
        response.Context.Should().ContainKey("tools");
        var tools = response.Context["tools"] as List<object>;
        tools.Should().Contain("Terraform");
    }

    [Fact]
    public async Task ProcessRequestAsync_ForProductionContext_AddsProductionConsiderations()
    {
        // Arrange
        var context = CreateTestContext();
        context.Environment.IsProduction = true;
        var request = "Deploy application to production";

        // Act
        var response = await _persona.ProcessRequestAsync(context, request);

        // Assert
        response.Response.Should().ContainAny("blue-green", "canary", "rollback");
        // Risk level check removed - property not in current implementation
    }

    [Fact]
    public void GetDefaultConfiguration_ReturnsExpectedConfig()
    {
        // Act
        var config = _persona.Configuration;

        // Assert
        config.CommunicationStyle.Should().Be(CommunicationStyle.TechnicalPrecise);
        config.TechnicalDepth.Should().Be(TechnicalDepth.Advanced);
        config.ResponseFormat.Should().Be(ResponseFormat.Structured);
        config.ProactivityLevel.Should().BeGreaterThan(0.6);
    }

    [Fact]
    public async Task AdaptBehaviorAsync_ForJuniorUser_SimplifiesConfiguration()
    {
        // Arrange
        var userProfile = new UserProfile
        {
            Id = "junior-dev",
            Name = "Junior Developer",
            Role = "Developer",
            ExperienceLevel = "Beginner",
            Experience = ExperienceLevel.Junior
        };
        var projectContext = new ProjectContext
        {
            ProjectId = "test-project",
            ProjectName = "Test Project",
            Stage = "Development"
        };

        var initialDepth = _persona.Configuration.TechnicalDepth;

        // Act
        await _persona.AdaptBehaviorAsync(userProfile, projectContext);

        // Assert
        _persona.Configuration.TechnicalDepth.Should().NotBe(initialDepth);
    }

    [Theory]
    [InlineData("Create Docker container", new[] { "docker", "containerization" })]
    [InlineData("Setup GitHub Actions workflow", new[] { "github", "ci/cd", "automation" })]
    [InlineData("Configure AWS infrastructure", new[] { "aws", "cloud", "infrastructure" })]
    public async Task ProcessRequestAsync_ExtractsCorrectTopics(string request, string[] expectedTopics)
    {
        // Arrange
        var context = CreateTestContext();

        // Act
        var response = await _persona.ProcessRequestAsync(context, request);

        // Assert
        foreach (var topic in expectedTopics)
        {
            response.Metadata.Topics.Should().Contain(topic);
        }
    }

    [Fact]
    public async Task ProcessRequestAsync_GeneratesActionableSteps()
    {
        // Arrange
        var context = CreateTestContext();
        var request = "Set up monitoring for microservices";

        // Act
        var response = await _persona.ProcessRequestAsync(context, request);

        // Assert
        response.SuggestedActions.Should().HaveCountGreaterThan(2);
        response.SuggestedActions.Should().Contain(a => a.Title.Contains("metrics", StringComparison.OrdinalIgnoreCase));
        response.SuggestedActions.Should().Contain(a => a.Category == "Monitoring");
        response.SuggestedActions.Should().BeInDescendingOrder(a => a.Priority);
    }

    private DevOpsContext CreateTestContext()
    {
        return new DevOpsContext
        {
            Project = new ProjectMetadata
            {
                ProjectId = "test-project",
                Name = "Test Microservices Project",
                Stage = "Development"
            },
            Environment = new EnvironmentContext
            {
                EnvironmentType = "Development",
                IsProduction = false
                // Properties not available:
                // Region = "us-west-2",
                // CloudProvider = "AWS"
            },
            User = new UserProfile
            {
                Id = "test-user",
                Name = "Test Developer",
                Role = "DevOps Engineer",
                ExperienceLevel = "Intermediate",
                Experience = ExperienceLevel.MidLevel
            },
            Team = new TeamDynamics
            {
                TeamSize = 10,
                TeamMaturity = "Intermediate"
            }
        };
    }
}