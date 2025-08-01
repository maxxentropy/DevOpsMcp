using DevOpsMcp.Domain.Personas;
using DevOpsMcp.Server.Tools.Personas;
using FluentAssertions;
using Moq;
using System.Text.Json;
using Xunit;

namespace DevOpsMcp.Server.Tests.Tools.Personas;

public class ManagePersonaMemoryToolTests
{
    private readonly Mock<IPersonaMemoryManager> _memoryManagerMock;
    private readonly ManagePersonaMemoryTool _tool;

    public ManagePersonaMemoryToolTests()
    {
        _memoryManagerMock = new Mock<IPersonaMemoryManager>();
        _tool = new ManagePersonaMemoryTool(_memoryManagerMock.Object);
    }

    [Fact]
    public void Name_ReturnsCorrectName()
    {
        // Assert
        _tool.Name.Should().Be("manage_persona_memory");
    }

    [Fact]
    public void Description_ReturnsCorrectDescription()
    {
        // Assert
        _tool.Description.Should().Contain("memory operations");
    }

    [Fact]
    public async Task ExecuteAsync_WithSnapshotOperation_ReturnsMemorySnapshot()
    {
        // Arrange
        var arguments = new ManagePersonaMemoryArguments
        {
            Operation = "snapshot",
            PersonaId = "devops-engineer"
        };

        var snapshot = new MemorySnapshot
        {
            PersonaId = "devops-engineer",
            TotalConversations = 10,
            TotalInteractions = 50,
            AverageInteractionsPerConversation = 5.0,
            LastInteraction = DateTime.UtcNow.AddHours(-1),
            MemorySize = 1024 * 1024,
            LearningInsights = new Dictionary<string, object>
            {
                ["total_patterns"] = 15,
                ["confidence_level"] = 0.85
            }
        };

        _memoryManagerMock.Setup(x => x.CreateMemorySnapshotAsync("devops-engineer"))
            .ReturnsAsync(snapshot);

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeFalse();
        
        var responseJson = result.Content[0].Text;
        responseJson.Should().Contain("totalConversations\":10");
        responseJson.Should().Contain("totalInteractions\":50");
        responseJson.Should().Contain("averageInteractionsPerConversation");
    }

    [Fact]
    public async Task ExecuteAsync_WithClearOperation_ClearsMemory()
    {
        // Arrange
        var arguments = new ManagePersonaMemoryArguments
        {
            Operation = "clear",
            PersonaId = "security-engineer",
            SessionId = "session123"
        };

        _memoryManagerMock.Setup(x => x.ClearMemoryAsync("security-engineer", "session123"))
            .ReturnsAsync(true);

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeFalse();
        result.Content[0].Text.Should().Contain("Successfully cleared memory");
        result.Content[0].Text.Should().Contain("session123");
    }

    [Fact]
    public async Task ExecuteAsync_WithClearAllSessions_ClearsAllPersonaMemory()
    {
        // Arrange
        var arguments = new ManagePersonaMemoryArguments
        {
            Operation = "clear",
            PersonaId = "sre-specialist"
            // No sessionId means clear all
        };

        _memoryManagerMock.Setup(x => x.ClearMemoryAsync("sre-specialist", null))
            .ReturnsAsync(true);

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeFalse();
        result.Content[0].Text.Should().Contain("Successfully cleared all memory");
        result.Content[0].Text.Should().Contain("sre-specialist");
    }

    [Fact]
    public async Task ExecuteAsync_WithCleanupOperation_PerformsCleanup()
    {
        // Arrange
        var arguments = new ManagePersonaMemoryArguments
        {
            Operation = "cleanup",
            RetentionDays = 7
        };

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeFalse();
        result.Content[0].Text.Should().Contain("Successfully cleaned up memories");
        result.Content[0].Text.Should().Contain("7 days");
        
        _memoryManagerMock.Verify(x => x.CleanupOldMemoriesAsync(
            It.Is<TimeSpan>(ts => ts.Days == 7)), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithMetricsOperation_ReturnsMetrics()
    {
        // Arrange
        var arguments = new ManagePersonaMemoryArguments
        {
            Operation = "metrics"
        };

        var metrics = new MemoryMetrics
        {
            ActiveContextCount = 25,
            CacheHitRate = 0.85,
            PersistentStorageSize = 10 * 1024 * 1024
        };
        
        // Manually populate the ContextsByPersona dictionary
        metrics.ContextsByPersona["devops-engineer"] = 10;
        metrics.ContextsByPersona["security-engineer"] = 8;
        metrics.ContextsByPersona["sre-specialist"] = 7;

        _memoryManagerMock.Setup(x => x.GetMemoryMetricsAsync())
            .ReturnsAsync(metrics);

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeFalse();
        
        var responseJson = result.Content[0].Text;
        responseJson.Should().Contain("activeContextCount\":25");
        responseJson.Should().Contain("cacheHitRate\":0.85");
        responseJson.Should().Contain("contextsByPersona");
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidOperation_ReturnsError()
    {
        // Arrange
        var arguments = new ManagePersonaMemoryArguments
        {
            Operation = "invalid-operation",
            PersonaId = "test"
        };

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("Unknown operation");
    }

    [Fact]
    public async Task ExecuteAsync_WithClearFailure_ReturnsError()
    {
        // Arrange
        var arguments = new ManagePersonaMemoryArguments
        {
            Operation = "clear",
            PersonaId = "test-persona"
        };

        _memoryManagerMock.Setup(x => x.ClearMemoryAsync("test-persona", null))
            .ReturnsAsync(false);

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("Failed to clear memory");
    }

    [Fact]
    public async Task ExecuteAsync_WithException_ReturnsError()
    {
        // Arrange
        var arguments = new ManagePersonaMemoryArguments
        {
            Operation = "snapshot",
            PersonaId = "test"
        };

        _memoryManagerMock.Setup(x => x.CreateMemorySnapshotAsync(It.IsAny<string>()))
            .ThrowsAsync(new Exception("Memory error"));

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("Error managing persona memory");
    }

    [Fact]
    public void InputSchema_ContainsRequiredProperties()
    {
        // Act
        var schema = _tool.InputSchema;

        // Assert
        schema.GetProperty("type").GetString().Should().Be("object");
        var properties = schema.GetProperty("properties");
        properties.TryGetProperty("operation", out _).Should().BeTrue();
        properties.TryGetProperty("personaId", out _).Should().BeTrue();
        properties.TryGetProperty("sessionId", out _).Should().BeTrue();
        properties.TryGetProperty("retentionDays", out _).Should().BeTrue();
        
        var required = schema.GetProperty("required");
        required.EnumerateArray().Select(e => e.GetString())
            .Should().Contain("operation");
    }
}