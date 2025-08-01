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

public class SecurityEngineerPersonaTests
{
    private readonly Mock<ILogger<SecurityEngineerPersona>> _loggerMock;
    private readonly Mock<IPersonaMemoryManager> _memoryManagerMock;
    private readonly Mock<IMediator> _mediatorMock;
    private readonly SecurityEngineerPersona _persona;

    public SecurityEngineerPersonaTests()
    {
        _loggerMock = new Mock<ILogger<SecurityEngineerPersona>>();
        _memoryManagerMock = new Mock<IPersonaMemoryManager>();
        _mediatorMock = new Mock<IMediator>();
        _persona = new SecurityEngineerPersona(_loggerMock.Object, _memoryManagerMock.Object, _mediatorMock.Object);
    }

    [Fact]
    public void Constructor_InitializesPersonaCorrectly()
    {
        // Assert
        _persona.Id.Should().Be("security-engineer");
        _persona.Name.Should().Be("Security Engineer");
        _persona.Role.Should().Be("DevSecOps Engineer");
        _persona.Specialization.Should().Be(DevOpsSpecialization.Security);
        _persona.Description.Should().Contain("security");
    }

    [Fact]
    public void InitializeCapabilities_SetsCorrectCapabilities()
    {
        // Assert
        _persona.Capabilities.Should().ContainKey("vulnerability_scanning");
        _persona.Capabilities.Should().ContainKey("security_assessment");
        _persona.Capabilities.Should().ContainKey("compliance_management");
        _persona.Capabilities.Should().ContainKey("threat_modeling");
        _persona.Capabilities.Should().ContainKey("incident_response");
        _persona.Capabilities.Should().ContainKey("security_automation");
        _persona.Capabilities.Should().ContainKey("policy_enforcement");
        _persona.Capabilities.Should().ContainKey("secret_management");
    }

    [Fact]
    public async Task ProcessRequestAsync_WithSecurityAudit_ReturnsDetailedAnalysis()
    {
        // Arrange
        var context = CreateTestContext();
        var request = "Perform a security audit of our deployment pipeline";

        // Act
        var response = await _persona.ProcessRequestAsync(context, request);

        // Assert
        response.Should().NotBeNull();
        response.Response.Should().Contain("security");
        response.Confidence.Overall.Should().BeGreaterThan(0.85);
        response.Context.Should().ContainKey("security_findings");
        response.Context.Should().ContainKey("risk_assessment");
        response.Metadata.IntentClassification.Should().Be("security_audit");
    }

    [Fact]
    public async Task ProcessRequestAsync_WithVulnerabilityScanning_IdentifiesRisks()
    {
        // Arrange
        var context = CreateTestContext();
        var request = "Scan our Docker images for vulnerabilities";

        // Act
        var response = await _persona.ProcessRequestAsync(context, request);

        // Assert
        response.Should().NotBeNull();
        response.Response.Should().Contain("vulnerabilit");
        response.SuggestedActions.Should().Contain(a => a.Category == "Security");
        response.Context.Should().ContainKey("scanning_strategy");
        // RequiresFollowUp property removed from ResponseMetadata
        // response.Metadata.RequiresFollowUp.Should().BeTrue();
    }

    [Fact]
    public async Task ProcessRequestAsync_WithCompliance_ProvidesFrameworkGuidance()
    {
        // Arrange
        var context = CreateTestContext();
        // ComplianceRequirements property removed from ProjectMetadata
        // context.Project.ComplianceRequirements = new List<string> { "SOC2", "HIPAA" };
        var request = "What do we need for SOC2 compliance?";

        // Act
        var response = await _persona.ProcessRequestAsync(context, request);

        // Assert
        response.Should().NotBeNull();
        response.Response.Should().Contain("SOC2");
        response.Context.Should().ContainKey("compliance_checklist");
        response.Context.Should().ContainKey("control_mappings");
        response.SuggestedActions.Should().Contain(a => a.Title.Contains("compliance", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CalculateRoleAlignmentAsync_WithSecurityTask_ReturnsHighScore()
    {
        // Arrange
        var task = new DevOpsTask
        {
            Title = "Security Scan",
            Description = "Critical infrastructure security scan",
            Category = TaskCategory.Security,
            Complexity = TaskComplexity.Complex
        };
        task.Parameters["priority"] = "critical";
        task.Parameters["scope"] = "infrastructure";

        // Act
        var score = await _persona.CalculateRoleAlignmentAsync(task);

        // Assert
        score.Should().BeGreaterThan(0.95);
    }

    [Fact]
    public async Task CalculateRoleAlignmentAsync_WithUITask_ReturnsLowScore()
    {
        // Arrange
        var task = new DevOpsTask
        {
            Title = "UI Design",
            Description = "Design user interface",
            Category = TaskCategory.Documentation, // Using closest available category
            Complexity = TaskComplexity.Simple
        };

        // Act
        var score = await _persona.CalculateRoleAlignmentAsync(task);

        // Assert
        score.Should().BeLessThan(0.3);
    }

    [Fact]
    public async Task ProcessRequestAsync_WithThreatModeling_GeneratesThreatModel()
    {
        // Arrange
        var context = CreateTestContext();
        var request = "Create a threat model for our new API";

        // Act
        var response = await _persona.ProcessRequestAsync(context, request);

        // Assert
        response.Should().NotBeNull();
        response.Response.Should().Contain("threat");
        response.Context.Should().ContainKey("threat_categories");
        response.Context.Should().ContainKey("mitigation_strategies");
        response.Context.Should().ContainKey("stride_analysis");
    }

    [Fact]
    public async Task ProcessRequestAsync_WithIncidentResponse_ProvidesSecurityProtocol()
    {
        // Arrange
        var context = CreateTestContext(isProduction: true);
        var request = "We detected suspicious activity in our logs";

        // Act
        var response = await _persona.ProcessRequestAsync(context, request);

        // Assert
        response.Should().NotBeNull();
        response.Response.Should().Contain("incident");
        response.Context.Should().ContainKey("incident_response_steps");
        response.Context.Should().ContainKey("containment_measures");
        response.SuggestedActions.Any(a => a.Priority == ActionPriority.Critical).Should().BeTrue();
    }

    [Fact]
    public async Task AdaptBehaviorAsync_WithHighComplianceProject_IncreasesDetailLevel()
    {
        // Arrange
        var userProfile = new UserProfile
        {
            Id = "compliance-officer",
            Name = "Compliance Officer",
            Role = "Compliance Officer"
            // PreferredCommunicationStyle property not available on UserProfile
        };
        var projectContext = new ProjectContext
        {
            ProjectId = "high-compliance-project",
            Stage = "Production"
            // Properties not available on ProjectContext:
            // ComplianceLevel = "high",
            // RegulatoryRequirements = { "PCI-DSS", "GDPR" }
        };

        // Act
        await _persona.AdaptBehaviorAsync(userProfile, projectContext);

        // Assert
        // ResponseDetailLevel property removed from PersonaConfiguration
        // _persona.Configuration.ResponseDetailLevel.Should().Be(DetailLevel.Comprehensive);
        _persona.Configuration.TechnicalDepth.Should().Be(TechnicalDepth.Expert);
    }

    [Fact]
    public async Task ProcessRequestAsync_WithSecretManagement_ProvidesSecureStrategy()
    {
        // Arrange
        var context = CreateTestContext();
        var request = "How should we manage secrets in our Kubernetes cluster?";

        // Act
        var response = await _persona.ProcessRequestAsync(context, request);

        // Assert
        response.Should().NotBeNull();
        response.Response.Should().Contain("secret");
        response.Context.Should().ContainKey("secret_management_tools");
        response.Context.Should().ContainKey("rotation_policy");
        response.SuggestedActions.Should().Contain(a => a.Category == "Security");
    }

    [Fact]
    public async Task ProcessRequestAsync_WithZeroTrust_DesignsArchitecture()
    {
        // Arrange
        var context = CreateTestContext();
        var request = "Design a zero-trust architecture for our microservices";

        // Act
        var response = await _persona.ProcessRequestAsync(context, request);

        // Assert
        response.Should().NotBeNull();
        response.Response.Should().Contain("zero-trust");
        response.Context.Should().ContainKey("architecture_principles");
        response.Context.Should().ContainKey("implementation_phases");
        response.Metadata.Topics.Should().Contain("security");
        response.Metadata.Topics.Should().Contain("architecture");
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
                // Properties not available on ProjectMetadata:
                // TechnologyStack - this belongs on ProjectContext
                // ComplianceRequirements
            },
            Environment = new EnvironmentContext
            {
                EnvironmentType = isProduction ? "Production" : "Development",
                IsProduction = isProduction
                // Properties not available:
                // Region - use Regions list instead
                // Resources is read-only
            },
            User = new UserProfile
            {
                Id = "test-user",
                Name = "Test User",
                Role = "Security Engineer",
                ExperienceLevel = "Senior",
                Experience = ExperienceLevel.Senior
                // Properties not available:
                // PreferredCommunicationStyle
                // SecurityClearance
            },
            Team = new TeamDynamics
            {
                TeamSize = 15,
                TeamMaturity = "Advanced"
                // Properties not available:
                // DevOpsMaturityLevel
                // SecurityMaturityLevel
            }
        };
    }
}