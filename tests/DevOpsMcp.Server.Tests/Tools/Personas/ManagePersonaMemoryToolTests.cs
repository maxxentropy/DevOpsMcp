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

        var snapshot = new PersonaMemorySnapshot
        {
            PersonaId = "devops-engineer",
            TotalConversations = 10,
            TotalInteractions = 50,
            AverageInteractionsPerConversation = 5.0,
            SuccessRate = 0.92,
            SnapshotTime = DateTime.UtcNow
        };

        // Add common topics
        snapshot.CommonTopics["deployment"] = 15;
        snapshot.CommonTopics["ci/cd"] = 12;

        // Add learning insights
        snapshot.LearningInsights["deployment_patterns"] = "User prefers automated deployments";
        snapshot.LearningInsights["tool_preference"] = "Jenkins and GitLab CI";

        _memoryManagerMock.Setup(x => x.CreateMemorySnapshotAsync("devops-engineer"))
            .ReturnsAsync(snapshot);

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeFalse();
        result.Content[0].Text.Should().Contain("devops-engineer");
        result.Content[0].Text.Should().Contain("50");
        result.Content[0].Text.Should().Contain("deployment");
    }

    [Fact]
    public async Task ExecuteAsync_WithClearOperation_ClearsMemory()
    {
        // Arrange
        var arguments = new ManagePersonaMemoryArguments
        {
            Operation = "clear",
            PersonaId = "security-engineer",
            SessionId = "session-123"
        };

        _memoryManagerMock.Setup(x => x.ClearMemoryAsync("security-engineer", "session-123"))
            .ReturnsAsync(true);

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeFalse();
        result.Content[0].Text.Should().Contain("Successfully cleared memory");
        result.Content[0].Text.Should().Contain("session-123");
    }

    [Fact]
    public async Task ExecuteAsync_WithStatsOperation_ReturnsStats()
    {
        // Arrange
        var arguments = new ManagePersonaMemoryArguments
        {
            Operation = "stats",
            PersonaId = "devops-engineer"
        };

        var stats = new PersonaMemoryStats
        {
            PersonaId = "devops-engineer",
            TotalConversations = 100,
            TotalInteractions = 500,
            MemoryUsageBytes = 1024 * 1024, // 1MB
            OldestMemory = DateTime.UtcNow.AddDays(-30),
            NewestMemory = DateTime.UtcNow.AddMinutes(-30),
            AverageSessionLength = 15.5
        };

        _memoryManagerMock.Setup(x => x.GetMemoryStatsAsync("devops-engineer"))
            .ReturnsAsync(stats);

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeFalse();
        result.Content[0].Text.Should().Contain("100");
        result.Content[0].Text.Should().Contain("500");
    }

    [Fact]
    public async Task ExecuteAsync_WithSessionsOperation_ReturnsSessions()
    {
        // Arrange
        var arguments = new ManagePersonaMemoryArguments
        {
            Operation = "sessions",
            PersonaId = "sre-specialist"
        };

        var sessions = new List<string> { "session-1", "session-2", "session-3" };

        _memoryManagerMock.Setup(x => x.GetActiveSessionsAsync("sre-specialist"))
            .ReturnsAsync(sessions);

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeFalse();
        result.Content[0].Text.Should().Contain("3 active sessions");
        result.Content[0].Text.Should().Contain("session-1");
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
            Timestamp = DateTime.UtcNow,
            ActiveContextCount = 50,
            TotalMemoryUsage = 1000 * 1024, // 1MB
            CacheHitRate = 0.85,
            CacheMissRate = 0.15,
            PersistentStorageSize = 15 * 1024 * 1024, // 15MB
            OldestContext = DateTime.UtcNow.AddDays(-30)
        };

        _memoryManagerMock.Setup(x => x.GetMemoryMetricsAsync())
            .ReturnsAsync(metrics);

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeFalse();
        result.Content[0].Text.Should().Contain("1000");
        result.Content[0].Text.Should().Contain("15.5");
    }

    [Fact]
    public async Task ExecuteAsync_WithCleanupOperation_CleansOldMemories()
    {
        // Arrange
        var arguments = new ManagePersonaMemoryArguments
        {
            Operation = "cleanup",
            RetentionDays = 7
        };

        _memoryManagerMock.Setup(x => x.CleanupOldMemoriesAsync(It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeFalse();
        result.Content[0].Text.Should().Contain("Successfully cleaned up memories older than 7 days");
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidOperation_ReturnsError()
    {
        // Arrange
        var arguments = new ManagePersonaMemoryArguments
        {
            Operation = "invalid",
            PersonaId = "test"
        };

        var jsonArgs = JsonSerializer.SerializeToElement(arguments);

        // Act
        var result = await _tool.ExecuteAsync(jsonArgs);

        // Assert
        result.Should().NotBeNull();
        result.IsError.Should().BeTrue();
        result.Content[0].Text.Should().Contain("Unknown operation: invalid");
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
            .ThrowsAsync(new InvalidOperationException("Test error"));

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
    }
}