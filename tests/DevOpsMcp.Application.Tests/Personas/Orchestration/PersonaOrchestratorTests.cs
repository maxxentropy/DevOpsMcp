using DevOpsMcp.Application.Personas;
using DevOpsMcp.Application.Personas.Orchestration;
using DevOpsMcp.Domain.Personas;
using DevOpsMcp.Domain.Personas.Orchestration;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using MediatR;

namespace DevOpsMcp.Application.Tests.Personas.Orchestration;

public sealed class PersonaOrchestratorTests : IDisposable
{
    private readonly Microsoft.Extensions.DependencyInjection.ServiceProvider _serviceProvider;
    private readonly PersonaOrchestrator _orchestrator;
    private readonly Mock<ILogger<PersonaOrchestrator>> _loggerMock;

    public PersonaOrchestratorTests()
    {
        _loggerMock = new Mock<ILogger<PersonaOrchestrator>>();
        
        var services = new ServiceCollection();
        services.AddSingleton(_loggerMock.Object);
        services.AddTransient<ILogger<DevOpsEngineerPersona>>(sp => Mock.Of<ILogger<DevOpsEngineerPersona>>());
        services.AddTransient<ILogger<SiteReliabilityEngineerPersona>>(sp => Mock.Of<ILogger<SiteReliabilityEngineerPersona>>());
        services.AddTransient<ILogger<SecurityEngineerPersona>>(sp => Mock.Of<ILogger<SecurityEngineerPersona>>());
        services.AddTransient<ILogger<EngineeringManagerPersona>>(sp => Mock.Of<ILogger<EngineeringManagerPersona>>());
        
        // Add mocks for IPersonaMemoryManager and IMediator
        services.AddSingleton<IPersonaMemoryManager>(Mock.Of<IPersonaMemoryManager>());
        services.AddSingleton<IMediator>(Mock.Of<IMediator>());
        
        services.AddScoped<DevOpsEngineerPersona>();
        services.AddScoped<SiteReliabilityEngineerPersona>();
        services.AddScoped<SecurityEngineerPersona>();
        services.AddScoped<EngineeringManagerPersona>();
        
        _serviceProvider = services.BuildServiceProvider();
        _orchestrator = new PersonaOrchestrator(_loggerMock.Object, _serviceProvider);
    }

    [Fact]
    public async Task SelectPersonaAsync_WithBestMatchMode_SelectsHighestScoringPersona()
    {
        // Arrange
        var context = CreateTestContext();
        var request = "Set up CI/CD pipeline";
        var criteria = new PersonaSelectionCriteria
        {
            SelectionMode = PersonaSelectionMode.BestMatch,
            MinimumConfidenceThreshold = 0.5
        };

        // Act
        var result = await _orchestrator.SelectPersonaAsync(context, request, criteria);

        // Assert
        result.Should().NotBeNull();
        result.PrimaryPersonaId.Should().Be("devops-engineer");
        result.Confidence.Should().BeGreaterThan(0.7);
        result.SelectionReason.Should().Contain("Best match");
    }

    [Fact]
    public async Task SelectPersonaAsync_WithSpecializationMode_SelectsCorrectSpecialist()
    {
        // Arrange
        var context = CreateTestContext();
        var request = "Perform security audit";
        var criteria = new PersonaSelectionCriteria
        {
            SelectionMode = PersonaSelectionMode.SpecializationBased,
            PreferredSpecializations = { DevOpsSpecialization.Security }
        };

        // Act
        var result = await _orchestrator.SelectPersonaAsync(context, request, criteria);

        // Assert
        result.PrimaryPersonaId.Should().Be("security-engineer");
        result.SelectionReason.Should().Contain("Specialization match");
    }

    [Fact]
    public async Task OrchestrateMultiPersonaResponseAsync_CombinesResponses()
    {
        // Arrange
        var context = CreateTestContext();
        var request = "Design secure CI/CD pipeline";
        var involvedPersonaIds = new List<string> { "devops-engineer", "security-engineer" };

        // Act
        var result = await _orchestrator.OrchestrateMultiPersonaResponseAsync(context, request, involvedPersonaIds);

        // Assert
        result.Should().NotBeNull();
        result.ConsolidatedResponse.Should().NotBeNullOrWhiteSpace();
        result.Contributions.Should().HaveCount(2);
        result.CombinedContext.Should().NotBeEmpty();
        result.Metrics.TotalDuration.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RouteRequestAsync_WithValidPersona_ReturnsResponse()
    {
        // Arrange
        var context = CreateTestContext();
        var request = "Deploy application";
        await _orchestrator.SetPersonaStatusAsync("devops-engineer", true);

        // Act
        var response = await _orchestrator.RouteRequestAsync("devops-engineer", context, request);

        // Assert
        response.Should().NotBeNull();
        response.Response.Should().NotBeNullOrWhiteSpace();
        response.Confidence.Overall.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RouteRequestAsync_WithInvalidPersona_ThrowsException()
    {
        // Arrange
        var context = CreateTestContext();
        var request = "Test request";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _orchestrator.RouteRequestAsync("invalid-persona", context, request));
    }

    [Fact]
    public async Task ResolveConflictsAsync_WithConsensusStrategy_FindsCommonElements()
    {
        // Arrange
        var responses = new List<PersonaResponse>
        {
            CreatePersonaResponse("devops-engineer", "Use Jenkins", 0.8),
            CreatePersonaResponse("sre-specialist", "Use GitLab CI", 0.7),
            CreatePersonaResponse("security-engineer", "Ensure secure pipelines", 0.9)
        };
        var strategy = ConflictResolutionStrategy.Consensus;

        // Act
        var resolution = await _orchestrator.ResolveConflictsAsync(responses, strategy);

        // Assert
        resolution.Should().NotBeNull();
        resolution.ResolutionMethod.Should().Be("Consensus");
        resolution.ResolvedResponse.Should().NotBeNullOrWhiteSpace();
        resolution.Confidence.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetActivePersonasAsync_ReturnsOnlyActivePersonas()
    {
        // Arrange
        await _orchestrator.SetPersonaStatusAsync("devops-engineer", true);
        await _orchestrator.SetPersonaStatusAsync("security-engineer", false);

        // Act
        var activePersonas = await _orchestrator.GetActivePersonasAsync();

        // Assert
        activePersonas.Should().NotBeEmpty();
        activePersonas.Should().Contain(p => p.PersonaId == "devops-engineer" && p.IsActive);
        activePersonas.Should().NotContain(p => p.PersonaId == "security-engineer" && p.IsActive);
    }

    [Fact]
    public async Task SetPersonaStatusAsync_UpdatesStatus()
    {
        // Act
        var result = await _orchestrator.SetPersonaStatusAsync("devops-engineer", false);

        // Assert
        result.Should().BeTrue();
        var activePersonas = await _orchestrator.GetActivePersonasAsync();
        activePersonas.Should().NotContain(p => p.PersonaId == "devops-engineer" && p.IsActive);
    }

    [Theory]
    [InlineData(PersonaSelectionMode.RoundRobin)]
    [InlineData(PersonaSelectionMode.LoadBalanced)]
    public async Task SelectPersonaAsync_WithDifferentModes_ReturnsValidSelection(PersonaSelectionMode mode)
    {
        // Arrange
        var context = CreateTestContext();
        var request = "Test request";
        var criteria = new PersonaSelectionCriteria { SelectionMode = mode };

        // Act
        var result = await _orchestrator.SelectPersonaAsync(context, request, criteria);

        // Assert
        result.Should().NotBeNull();
        result.PrimaryPersonaId.Should().NotBeNullOrWhiteSpace();
        result.Confidence.Should().BeGreaterThan(0);
    }

    private DevOpsContext CreateTestContext()
    {
        return new DevOpsContext
        {
            Project = new ProjectMetadata
            {
                ProjectId = "test-project",
                Name = "Test Project",
                Stage = "Development"
            },
            Environment = new EnvironmentContext
            {
                EnvironmentType = "Development",
                IsProduction = false
            },
            User = new UserProfile
            {
                Id = "test-user",
                Name = "Test User",
                Role = "Developer",
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

    private PersonaResponse CreatePersonaResponse(string personaId, string response, double confidence)
    {
        var personaResponse = new PersonaResponse
        {
            ResponseId = Guid.NewGuid().ToString(),
            PersonaId = personaId,
            Response = response,
            Confidence = new PersonaConfidence
            {
                Overall = confidence,
                DomainExpertise = confidence,
                ContextRelevance = confidence,
                ResponseQuality = confidence
            }
        };
        
        // Add suggested actions after creation since it's read-only
        personaResponse.SuggestedActions.Add(new SuggestedAction
        {
            Title = "Test Action",
            Description = "Test",
            Category = "Configuration",
            Priority = ActionPriority.Medium
        });
        
        return personaResponse;
    }

    public void Dispose()
    {
        _serviceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }
}