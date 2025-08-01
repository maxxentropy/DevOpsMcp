using DevOpsMcp.Application.Personas;
using DevOpsMcp.Domain.Personas;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DevOpsMcp.Application.Tests.Personas;

public class EngineeringManagerPersonaTests
{
    private readonly Mock<ILogger<EngineeringManagerPersona>> _loggerMock;
    private readonly EngineeringManagerPersona _persona;

    public EngineeringManagerPersonaTests()
    {
        _loggerMock = new Mock<ILogger<EngineeringManagerPersona>>();
        _persona = new EngineeringManagerPersona(_loggerMock.Object);
    }

    [Fact]
    public void Constructor_InitializesPersonaCorrectly()
    {
        // Assert
        _persona.Id.Should().Be("engineering-manager");
        _persona.Name.Should().Be("Engineering Manager");
        _persona.Role.Should().Be("Engineering Manager");
        _persona.Specialization.Should().Be(DevOpsSpecialization.Management);
        _persona.Description.Should().Contain("strategic");
    }

    [Fact]
    public void InitializeCapabilities_SetsCorrectCapabilities()
    {
        // Assert
        _persona.Capabilities.Should().ContainKey("team_management");
        _persona.Capabilities.Should().ContainKey("process_optimization");
        _persona.Capabilities.Should().ContainKey("strategic_planning");
        _persona.Capabilities.Should().ContainKey("resource_allocation");
        _persona.Capabilities.Should().ContainKey("stakeholder_communication");
        _persona.Capabilities.Should().ContainKey("metrics_reporting");
        _persona.Capabilities.Should().ContainKey("risk_management");
        _persona.Capabilities.Should().ContainKey("culture_building");
    }

    [Fact]
    public async Task ProcessRequestAsync_WithTeamManagement_ProvidesLeadershipGuidance()
    {
        // Arrange
        var context = CreateTestContext();
        var request = "How can I improve my team's DevOps practices?";

        // Act
        var response = await _persona.ProcessRequestAsync(context, request);

        // Assert
        response.Should().NotBeNull();
        response.Response.Should().Contain("team");
        response.Confidence.Overall.Should().BeGreaterThan(0.8);
        response.Context.Should().ContainKey("improvement_strategies");
        response.Context.Should().ContainKey("team_assessment");
        response.Metadata.IntentClassification.Should().Be("team_improvement");
    }

    [Fact]
    public async Task ProcessRequestAsync_WithProcessOptimization_AnalyzesWorkflow()
    {
        // Arrange
        var context = CreateTestContext();
        context.Team.CurrentChallenges = new List<string> { "slow deployments", "manual processes" };
        var request = "Our deployment process is too slow and error-prone";

        // Act
        var response = await _persona.ProcessRequestAsync(context, request);

        // Assert
        response.Should().NotBeNull();
        response.Response.Should().Contain("process");
        response.Context.Should().ContainKey("process_improvements");
        response.Context.Should().ContainKey("automation_opportunities");
        response.SuggestedActions.Should().Contain(a => a.Category == ActionCategory.Process);
    }

    [Fact]
    public async Task ProcessRequestAsync_WithMetricsReporting_ProvidesKPIs()
    {
        // Arrange
        var context = CreateTestContext();
        var request = "What metrics should I track for our DevOps transformation?";

        // Act
        var response = await _persona.ProcessRequestAsync(context, request);

        // Assert
        response.Should().NotBeNull();
        response.Response.Should().Contain("metrics");
        response.Context.Should().ContainKey("key_metrics");
        response.Context.Should().ContainKey("measurement_framework");
        response.Context.Should().ContainKey("reporting_cadence");
        response.Metadata.Topics.Should().Contain("metrics");
    }

    [Fact]
    public async Task CalculateRoleAlignmentAsync_WithManagementTask_ReturnsHighScore()
    {
        // Arrange
        var task = new DevOpsTask
        {
            Type = "team_planning",
            Category = "management",
            Context = new Dictionary<string, object>
            {
                ["scope"] = "quarterly planning",
                ["team_size"] = 20
            }
        };

        // Act
        var score = await _persona.CalculateRoleAlignmentAsync(task);

        // Assert
        score.Should().BeGreaterThan(0.9);
    }

    [Fact]
    public async Task CalculateRoleAlignmentAsync_WithCodingTask_ReturnsLowerScore()
    {
        // Arrange
        var task = new DevOpsTask
        {
            Type = "code_implementation",
            Category = "development",
            Context = new Dictionary<string, object>()
        };

        // Act
        var score = await _persona.CalculateRoleAlignmentAsync(task);

        // Assert
        score.Should().BeLessThan(0.4);
    }

    [Fact]
    public async Task ProcessRequestAsync_WithResourceAllocation_OptimizesTeamStructure()
    {
        // Arrange
        var context = CreateTestContext();
        context.Team.TeamSize = 25;
        context.Team.SkillGaps = new List<string> { "cloud architecture", "security" };
        var request = "How should I structure my team for our cloud migration?";

        // Act
        var response = await _persona.ProcessRequestAsync(context, request);

        // Assert
        response.Should().NotBeNull();
        response.Response.Should().Contain("team structure");
        response.Context.Should().ContainKey("team_topology");
        response.Context.Should().ContainKey("skill_requirements");
        response.Context.Should().ContainKey("hiring_recommendations");
    }

    [Fact]
    public async Task ProcessRequestAsync_WithStakeholderCommunication_CreatesStrategy()
    {
        // Arrange
        var context = CreateTestContext();
        var request = "How do I communicate our DevOps progress to executives?";

        // Act
        var response = await _persona.ProcessRequestAsync(context, request);

        // Assert
        response.Should().NotBeNull();
        response.Response.Should().Contain("communication");
        response.Context.Should().ContainKey("communication_plan");
        response.Context.Should().ContainKey("executive_metrics");
        response.SuggestedActions.Should().Contain(a => a.Title.Contains("report", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AdaptBehaviorAsync_WithTechnicalAudience_AdjustsDepth()
    {
        // Arrange
        var userProfile = new UserProfile
        {
            Id = "tech-lead",
            Role = "Technical Lead",
            ExperienceLevel = "Expert",
            Experience = ExperienceLevel.Expert
        };
        var projectContext = new ProjectContext
        {
            ProjectId = "technical-project",
            TechnicalComplexity = "high"
        };

        // Act
        await _persona.AdaptBehaviorAsync(userProfile, projectContext);

        // Assert
        _persona.Configuration.TechnicalDepth.Should().Be(TechnicalDepth.Advanced);
        _persona.Configuration.IncludeImplementationDetails.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessRequestAsync_WithRiskManagement_IdentifiesRisks()
    {
        // Arrange
        var context = CreateTestContext();
        var request = "What are the risks of adopting microservices?";

        // Act
        var response = await _persona.ProcessRequestAsync(context, request);

        // Assert
        response.Should().NotBeNull();
        response.Response.Should().Contain("risk");
        response.Context.Should().ContainKey("risk_matrix");
        response.Context.Should().ContainKey("mitigation_strategies");
        response.Context.Should().ContainKey("decision_framework");
        response.Metadata.RequiresFollowUp.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessRequestAsync_WithCultureBuilding_ProvidesStrategies()
    {
        // Arrange
        var context = CreateTestContext();
        context.Team.CultureChallenges = new List<string> { "silos", "resistance to change" };
        var request = "How can we build a strong DevOps culture?";

        // Act
        var response = await _persona.ProcessRequestAsync(context, request);

        // Assert
        response.Should().NotBeNull();
        response.Response.Should().Contain("culture");
        response.Context.Should().ContainKey("culture_initiatives");
        response.Context.Should().ContainKey("change_management");
        response.Context.Should().ContainKey("success_metrics");
        response.SuggestedActions.Any(a => a.Category == ActionCategory.Process).Should().BeTrue();
    }

    [Fact]
    public async Task ProcessRequestAsync_WithBudgetPlanning_ProvidesCostAnalysis()
    {
        // Arrange
        var context = CreateTestContext();
        var request = "Help me plan the DevOps tooling budget for next year";

        // Act
        var response = await _persona.ProcessRequestAsync(context, request);

        // Assert
        response.Should().NotBeNull();
        response.Response.Should().Contain("budget");
        response.Context.Should().ContainKey("cost_breakdown");
        response.Context.Should().ContainKey("roi_analysis");
        response.Context.Should().ContainKey("vendor_comparison");
        response.Metadata.Topics.Should().Contain("planning");
    }

    private DevOpsContext CreateTestContext()
    {
        return new DevOpsContext
        {
            Project = new ProjectMetadata
            {
                ProjectId = "test-project",
                Name = "Enterprise Platform",
                Stage = "Growth",
                TechnologyStack = new TechnologyConfiguration
                {
                    Languages = { "Java", "Python", "Go" },
                    Frameworks = { "Spring Boot", "Kubernetes", "Terraform" },
                    CloudProviders = { "AWS", "Azure" },
                    Tools = { "Jenkins", "GitLab", "Datadog" }
                },
                Budget = 1000000,
                Timeline = "12 months"
            },
            Environment = new EnvironmentContext
            {
                EnvironmentType = "Enterprise",
                IsProduction = false,
                Region = "global",
                Resources = new Dictionary<string, object>
                {
                    ["team_size"] = 50,
                    ["environments"] = new[] { "dev", "staging", "prod" }
                }
            },
            User = new UserProfile
            {
                Id = "manager-user",
                Name = "Manager User",
                Role = "Engineering Manager",
                ExperienceLevel = "Expert",
                Experience = ExperienceLevel.Expert,
                PreferredCommunicationStyle = PreferredCommunicationStyle.Strategic,
                ManagementLevel = "senior"
            },
            Team = new TeamContext
            {
                TeamSize = 30,
                DevOpsMaturityLevel = "Intermediate",
                ReportingStructure = "matrix",
                Locations = new List<string> { "US", "EU", "APAC" },
                CurrentChallenges = new List<string>(),
                SkillGaps = new List<string>(),
                CultureChallenges = new List<string>()
            }
        };
    }
}