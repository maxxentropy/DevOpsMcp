using DevOpsMcp.Application.Personas;
using DevOpsMcp.Domain.Personas;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using MediatR;
using System;
using System.Collections.Generic;

namespace DevOpsMcp.Application.Tests.Personas;

public class SiteReliabilityEngineerPersonaTests
{
    private readonly Mock<ILogger<SiteReliabilityEngineerPersona>> _loggerMock;
    private readonly Mock<IPersonaMemoryManager> _memoryManagerMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly SiteReliabilityEngineerPersona _persona;

    public SiteReliabilityEngineerPersonaTests()
    {
        _loggerMock = new Mock<ILogger<SiteReliabilityEngineerPersona>>();
        _memoryManagerMock = new Mock<IPersonaMemoryManager>();
        _mediatorMock = new Mock<IMediator>();
        _persona = new SiteReliabilityEngineerPersona(_loggerMock.Object, _memoryManagerMock.Object, _mediatorMock.Object);
    }

    [Fact]
    public void Constructor_InitializesPersonaCorrectly()
    {
        // Assert
        _persona.Id.Should().Be("sre-specialist");
        _persona.Name.Should().Be("Site Reliability Engineer");
        _persona.Role.Should().Be("Reliability and Performance Specialist");
        _persona.Specialization.Should().Be(DevOpsSpecialization.Reliability);
        _persona.Description.Should().Contain("reliability");
    }

    [Fact]
    public void InitializeCapabilities_SetsCorrectCapabilities()
    {
        // Assert
        _persona.Capabilities.Should().ContainKey("monitoring");
        _persona.Capabilities.Should().ContainKey("incident_response");
        _persona.Capabilities.Should().ContainKey("slo_management");
        _persona.Capabilities.Should().ContainKey("chaos_engineering");
        _persona.Capabilities.Should().ContainKey("capacity_planning");
        _persona.Capabilities.Should().ContainKey("performance_optimization");
        _persona.Capabilities.Should().ContainKey("automation");
        _persona.Capabilities.Should().ContainKey("postmortem_analysis");
    }

    [Fact]
    public async Task ProcessRequestAsync_WithMonitoringRequest_ReturnsHighConfidence()
    {
        // Arrange
        var context = CreateTestContext();
        var request = "Set up monitoring and alerting for our microservices";

        // Act
        var response = await _persona.ProcessRequestAsync(context, request);

        // Assert
        response.Should().NotBeNull();
        response.Response.Should().Contain("monitoring");
        response.Confidence.Overall.Should().BeGreaterThan(0.8);
        response.SuggestedActions.Should().NotBeEmpty();
        response.Metadata.IntentClassification.Should().Be("monitoring_setup");
    }

    [Fact]
    public async Task ProcessRequestAsync_WithIncidentResponse_GeneratesRunbook()
    {
        // Arrange
        var context = CreateTestContext(isProduction: true);
        var request = "Database is experiencing high latency";

        // Act
        var response = await _persona.ProcessRequestAsync(context, request);

        // Assert
        response.Should().NotBeNull();
        response.Response.Should().Contain("incident");
        response.Context.Should().ContainKey("runbook_steps");
        response.Context.Should().ContainKey("escalation_policy");
        response.SuggestedActions.Any(a => a.Category == "Incident").Should().BeTrue();
    }

    [Fact]
    public async Task ProcessRequestAsync_WithSLOManagement_ProvideSLOGuidance()
    {
        // Arrange
        var context = CreateTestContext();
        var request = "How should we define SLOs for our API?";

        // Act
        var response = await _persona.ProcessRequestAsync(context, request);

        // Assert
        response.Should().NotBeNull();
        response.Response.Should().Contain("SLO");
        response.Context.Should().ContainKey("slo_recommendations");
        response.Metadata.Topics.Should().Contain("reliability");
    }

    [Fact]
    public async Task CalculateRoleAlignmentAsync_WithReliabilityTask_ReturnsHighScore()
    {
        // Arrange
        var task = new DevOpsTask
        {
            Title = "Incident Response",
            Description = "Production incident requiring immediate response",
            Category = TaskCategory.Troubleshooting,
            Complexity = TaskComplexity.Expert
        };
        task.Parameters["severity"] = "high";
        task.Parameters["environment"] = "production";

        // Act
        var score = await _persona.CalculateRoleAlignmentAsync(task);

        // Assert
        score.Should().BeGreaterThan(0.9);
    }

    [Fact]
    public async Task CalculateRoleAlignmentAsync_WithSecurityTask_ReturnsLowerScore()
    {
        // Arrange
        var task = new DevOpsTask
        {
            Title = "Security Audit",
            Description = "Perform security audit",
            Category = TaskCategory.Security,
            Complexity = TaskComplexity.Moderate
        };

        // Act
        var score = await _persona.CalculateRoleAlignmentAsync(task);

        // Assert
        score.Should().BeLessThan(0.5);
    }

    [Fact]
    public async Task AdaptBehaviorAsync_WithHighStressUser_AdaptsApproach()
    {
        // Arrange
        var userProfile = new UserProfile
        {
            Id = "stressed-user",
            Name = "Stressed User",
            Role = "Developer"
            // Properties removed from UserProfile:
            // PreferredCommunicationStyle = PreferredCommunicationStyle.Concise,
            // CurrentStressLevel = "high"
        };
        var projectContext = new ProjectContext
        {
            ProjectId = "critical-project",
            CurrentPhase = "incident"
        };

        // Act
        await _persona.AdaptBehaviorAsync(userProfile, projectContext);

        // Assert
        _persona.Configuration.CommunicationStyle.Should().Be(CommunicationStyle.Concise);
        // ResponseDetailLevel property removed from PersonaConfiguration
        // _persona.Configuration.ResponseDetailLevel.Should().Be(DetailLevel.Essential);
    }

    [Fact]
    public async Task ProcessRequestAsync_WithChaosEngineering_ProvidesExperimentDesign()
    {
        // Arrange
        var context = CreateTestContext();
        var request = "Design chaos engineering experiments for our payment service";

        // Act
        var response = await _persona.ProcessRequestAsync(context, request);

        // Assert
        response.Should().NotBeNull();
        response.Response.Should().Contain("chaos");
        response.Context.Should().ContainKey("experiment_design");
        response.Context.Should().ContainKey("blast_radius");
        response.SuggestedActions.Should().Contain(a => a.Title.Contains("experiment", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ProcessRequestAsync_WithCapacityPlanning_AnalyzesResources()
    {
        // Arrange
        var context = CreateTestContext();
        context.Environment.Resources["current_utilization"] = "85%";
        var request = "We need to plan capacity for Black Friday";

        // Act
        var response = await _persona.ProcessRequestAsync(context, request);

        // Assert
        response.Should().NotBeNull();
        response.Response.Should().Contain("capacity");
        response.Context.Should().ContainKey("scaling_recommendations");
        response.Context.Should().ContainKey("resource_projections");
        // RequiresFollowUp property removed from ResponseMetadata
        // response.Metadata.RequiresFollowUp.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessRequestAsync_WithPostmortem_GeneratesTemplate()
    {
        // Arrange
        var context = CreateTestContext();
        var request = "We need to conduct a postmortem for yesterday's outage";

        // Act
        var response = await _persona.ProcessRequestAsync(context, request);

        // Assert
        response.Should().NotBeNull();
        response.Response.Should().Contain("postmortem");
        response.Context.Should().ContainKey("postmortem_template");
        response.Context.Should().ContainKey("timeline_guidance");
        response.SuggestedActions.Should().Contain(a => a.Category == "Documentation");
    }

    private DevOpsContext CreateTestContext(bool isProduction = false)
    {
        return new DevOpsContext
        {
            Project = new ProjectMetadata
            {
                ProjectId = "test-project",
                Name = "Test Project",
                Stage = isProduction ? "Production" : "Development"
            },
            Environment = new EnvironmentContext
            {
                EnvironmentType = isProduction ? "Production" : "Development",
                IsProduction = isProduction
                // Properties not available:
                // Region = "us-east-1" - use Regions list instead
                // Resources is read-only - cannot assign dictionary
            },
            User = new UserProfile
            {
                Id = "test-user",
                Name = "Test User",
                Role = "SRE",
                ExperienceLevel = "Senior",
                Experience = ExperienceLevel.Senior,
                // PreferredCommunicationStyle property not available on UserProfile
            },
            Team = new TeamDynamics
            {
                TeamSize = 8,
                TeamMaturity = "Advanced"
                // OnCallRotationSize property not available
            }
        };
    }
}