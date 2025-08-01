using DevOpsMcp.Application.Personas;
using DevOpsMcp.Domain.Personas;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DevOpsMcp.Application.Tests.Personas;

public class SiteReliabilityEngineerPersonaTests
{
    private readonly Mock<ILogger<SiteReliabilityEngineerPersona>> _loggerMock;
    private readonly SiteReliabilityEngineerPersona _persona;

    public SiteReliabilityEngineerPersonaTests()
    {
        _loggerMock = new Mock<ILogger<SiteReliabilityEngineerPersona>>();
        _persona = new SiteReliabilityEngineerPersona(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_InitializesPersonaCorrectly()
    {
        // Assert
        _persona.Id.Should().Be("sre-specialist");
        _persona.Name.Should().Be("SRE Specialist");
        _persona.Role.Should().Be("Site Reliability Engineer");
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
        response.SuggestedActions.Any(a => a.Category == ActionCategory.Incident).Should().BeTrue();
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
            Type = "incident_response",
            Category = "reliability",
            Context = new Dictionary<string, object>
            {
                ["severity"] = "high",
                ["environment"] = "production"
            }
        };

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
            Type = "security_audit",
            Category = "security",
            Context = new Dictionary<string, object>()
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
            PreferredCommunicationStyle = PreferredCommunicationStyle.Concise,
            CurrentStressLevel = "high"
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
        _persona.Configuration.ResponseDetailLevel.Should().Be(DetailLevel.Essential);
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
        response.Metadata.RequiresFollowUp.Should().BeTrue();
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
        response.SuggestedActions.Should().Contain(a => a.Category == ActionCategory.Documentation);
    }

    private DevOpsContext CreateTestContext(bool isProduction = false)
    {
        return new DevOpsContext
        {
            Project = new ProjectMetadata
            {
                ProjectId = "test-project",
                Name = "Test Project",
                Stage = isProduction ? "Production" : "Development",
                TechnologyStack = new TechnologyConfiguration
                {
                    Languages = { "Go", "Python" },
                    Frameworks = { "Kubernetes", "Prometheus" },
                    CloudProviders = { "AWS" },
                    Tools = { "Grafana", "PagerDuty" }
                }
            },
            Environment = new EnvironmentContext
            {
                EnvironmentType = isProduction ? "Production" : "Development",
                IsProduction = isProduction,
                Region = "us-east-1",
                Resources = new Dictionary<string, object>
                {
                    ["monitoring_tools"] = new[] { "Prometheus", "Grafana" },
                    ["incident_management"] = "PagerDuty"
                }
            },
            User = new UserProfile
            {
                Id = "test-user",
                Name = "Test User",
                Role = "SRE",
                ExperienceLevel = "Senior",
                Experience = ExperienceLevel.Senior,
                PreferredCommunicationStyle = PreferredCommunicationStyle.Detailed
            },
            Team = new TeamContext
            {
                TeamSize = 8,
                DevOpsMaturityLevel = "Advanced",
                OnCallRotationSize = 4
            }
        };
    }
}