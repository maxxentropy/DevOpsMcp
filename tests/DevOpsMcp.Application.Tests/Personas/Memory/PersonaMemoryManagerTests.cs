using DevOpsMcp.Application.Personas.Memory;
using DevOpsMcp.Domain.Personas;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using System.Text;
using System.Text.Json;

namespace DevOpsMcp.Application.Tests.Personas.Memory;

public class PersonaMemoryManagerTests
{
    private readonly Mock<ILogger<PersonaMemoryManager>> _loggerMock;
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly Mock<IPersonaMemoryStore> _storeMock;
    private readonly PersonaMemoryManager _memoryManager;

    public PersonaMemoryManagerTests()
    {
        _loggerMock = new Mock<ILogger<PersonaMemoryManager>>();
        _cacheMock = new Mock<IDistributedCache>();
        _storeMock = new Mock<IPersonaMemoryStore>();
        _memoryManager = new PersonaMemoryManager(_loggerMock.Object, _cacheMock.Object, _storeMock.Object);
    }

    [Fact]
    public async Task RetrieveConversationContextAsync_FromActiveMemory_ReturnsContext()
    {
        // Arrange
        var personaId = "test-persona";
        var sessionId = "test-session";
        var testContext = CreateTestConversationContext(personaId, sessionId);
        
        // First store it
        await _memoryManager.StoreConversationContextAsync(personaId, testContext);

        // Act
        var result = await _memoryManager.RetrieveConversationContextAsync(personaId, sessionId);

        // Assert
        result.Should().NotBeNull();
        result!.SessionId.Should().Be(sessionId);
        result.PersonaId.Should().Be(personaId);
    }

    [Fact]
    public async Task RetrieveConversationContextAsync_FromCache_ReturnsContext()
    {
        // Arrange
        var personaId = "test-persona";
        var sessionId = "test-session";
        var context = CreateTestConversationContext(personaId, sessionId);
        var contextJson = JsonSerializer.Serialize(context);
        
        _cacheMock.Setup(x => x.GetStringAsync(It.IsAny<string>(), default))
            .ReturnsAsync(contextJson);

        // Act
        var result = await _memoryManager.RetrieveConversationContextAsync(personaId, sessionId);

        // Assert
        result.Should().NotBeNull();
        result!.SessionId.Should().Be(sessionId);
        _cacheMock.Verify(x => x.GetStringAsync(It.IsAny<string>(), default), Times.Once);
    }

    [Fact]
    public async Task RetrieveConversationContextAsync_FromPersistentStore_ReturnsContext()
    {
        // Arrange
        var personaId = "test-persona";
        var sessionId = "test-session";
        var context = CreateTestConversationContext(personaId, sessionId);
        
        _cacheMock.Setup(x => x.GetStringAsync(It.IsAny<string>(), default))
            .ReturnsAsync((string)null!);
        _storeMock.Setup(x => x.LoadContextAsync(personaId, sessionId))
            .ReturnsAsync(context);

        // Act
        var result = await _memoryManager.RetrieveConversationContextAsync(personaId, sessionId);

        // Assert
        result.Should().NotBeNull();
        result!.SessionId.Should().Be(sessionId);
        _storeMock.Verify(x => x.LoadContextAsync(personaId, sessionId), Times.Once);
    }

    [Fact]
    public async Task StoreConversationContextAsync_StoresInAllLayers()
    {
        // Arrange
        var personaId = "test-persona";
        var context = CreateTestConversationContext(personaId, "test-session");

        // Act
        await _memoryManager.StoreConversationContextAsync(personaId, context);

        // Assert
        _cacheMock.Verify(x => x.SetStringAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            default), Times.Once);
        
        // Note: Persistent store save is done async in background
        await Task.Delay(100); // Give background task time to execute
        _storeMock.Verify(x => x.SaveContextAsync(personaId, context), Times.Once);
    }

    [Fact]
    public async Task CreateMemorySnapshotAsync_GeneratesAccurateSnapshot()
    {
        // Arrange
        var personaId = "test-persona";
        var contexts = new List<ConversationContext>
        {
            CreateTestConversationContext(personaId, "session1", interactionCount: 5),
            CreateTestConversationContext(personaId, "session2", interactionCount: 3)
        };

        foreach (var ctx in contexts)
        {
            await _memoryManager.StoreConversationContextAsync(personaId, ctx);
        }

        var learningData = new LearningData
        {
            PatternCount = 10,
            AdaptationConfidence = 0.85,
            LastUpdate = DateTime.UtcNow
        };
        _storeMock.Setup(x => x.GetLearningDataAsync(personaId))
            .ReturnsAsync(learningData);

        // Act
        var snapshot = await _memoryManager.CreateMemorySnapshotAsync(personaId);

        // Assert
        snapshot.Should().NotBeNull();
        snapshot.PersonaId.Should().Be(personaId);
        snapshot.TotalConversations.Should().Be(2);
        snapshot.TotalInteractions.Should().Be(8);
        snapshot.AverageInteractionsPerConversation.Should().Be(4.0);
        snapshot.LearningInsights.Should().ContainKey("total_learned_patterns");
        snapshot.LearningInsights["total_learned_patterns"].Should().Be(10);
    }

    [Fact]
    public async Task ClearMemoryAsync_WithSessionId_ClearsSpecificSession()
    {
        // Arrange
        var personaId = "test-persona";
        var sessionId = "test-session";

        // Act
        var result = await _memoryManager.ClearMemoryAsync(personaId, sessionId);

        // Assert
        result.Should().BeTrue();
        _cacheMock.Verify(x => x.RemoveAsync(It.IsAny<string>(), default), Times.Once);
        _storeMock.Verify(x => x.DeleteContextAsync(personaId, sessionId), Times.Once);
    }

    [Fact]
    public async Task ClearMemoryAsync_WithoutSessionId_ClearsAllPersonaSessions()
    {
        // Arrange
        var personaId = "test-persona";
        
        // Store some contexts first
        await _memoryManager.StoreConversationContextAsync(personaId, 
            CreateTestConversationContext(personaId, "session1"));
        await _memoryManager.StoreConversationContextAsync(personaId, 
            CreateTestConversationContext(personaId, "session2"));

        // Act
        var result = await _memoryManager.ClearMemoryAsync(personaId);

        // Assert
        result.Should().BeTrue();
        _cacheMock.Verify(x => x.RemoveAsync(It.IsAny<string>(), default), Times.AtLeastOnce);
        _storeMock.Verify(x => x.ClearPersonaDataAsync(personaId), Times.Once);
    }

    [Fact]
    public async Task GetActiveSessionsAsync_ReturnsAllSessions()
    {
        // Arrange
        var personaId = "test-persona";
        var persistedSessions = new List<string> { "session3", "session4" };
        
        await _memoryManager.StoreConversationContextAsync(personaId, 
            CreateTestConversationContext(personaId, "session1"));
        await _memoryManager.StoreConversationContextAsync(personaId, 
            CreateTestConversationContext(personaId, "session2"));
        
        _storeMock.Setup(x => x.GetSessionIdsAsync(personaId))
            .ReturnsAsync(persistedSessions);

        // Act
        var sessions = await _memoryManager.GetActiveSessionsAsync(personaId);

        // Assert
        sessions.Should().NotBeNull();
        sessions.Should().Contain(new[] { "session1", "session2", "session3", "session4" });
        sessions.Distinct().Count().Should().Be(4);
    }

    [Fact]
    public async Task GetMemoryMetricsAsync_ReturnsAccurateMetrics()
    {
        // Arrange
        await _memoryManager.StoreConversationContextAsync("persona1", 
            CreateTestConversationContext("persona1", "session1"));
        await _memoryManager.StoreConversationContextAsync("persona2", 
            CreateTestConversationContext("persona2", "session2"));

        var storageMetrics = new StorageMetrics
        {
            TotalSize = 1024 * 1024,
            OldestEntry = DateTime.UtcNow.AddDays(-30),
            TotalEntries = 100
        };
        _storeMock.Setup(x => x.GetStorageMetricsAsync())
            .ReturnsAsync(storageMetrics);

        // Act
        var metrics = await _memoryManager.GetMemoryMetricsAsync();

        // Assert
        metrics.Should().NotBeNull();
        metrics.ActiveContextCount.Should().Be(2);
        metrics.PersistentStorageSize.Should().Be(1024 * 1024);
        metrics.ContextsByPersona.Should().HaveCount(2);
        metrics.ContextsByPersona["persona1"].Should().Be(1);
        metrics.ContextsByPersona["persona2"].Should().Be(1);
    }

    [Fact]
    public async Task UpdatePersonaLearningAsync_SavesLearningData()
    {
        // Arrange
        var personaId = "test-persona";
        var learning = new PersonaLearning
        {
            PersonaId = personaId,
            Type = LearningType.UserPreference,
            Subject = "Communication Style",
            ConfidenceScore = 0.9
        };

        // Act
        await _memoryManager.UpdatePersonaLearningAsync(personaId, learning);

        // Assert
        _storeMock.Verify(x => x.SaveLearningDataAsync(personaId, learning), Times.Once);
    }

    [Fact]
    public async Task CleanupOldMemoriesAsync_RemovesExpiredContexts()
    {
        // Arrange
        var retention = TimeSpan.FromDays(7);
        var oldContext = CreateTestConversationContext("persona1", "old-session");
        oldContext.LastInteraction = DateTime.UtcNow.AddDays(-10);
        
        await _memoryManager.StoreConversationContextAsync("persona1", oldContext);
        await _memoryManager.StoreConversationContextAsync("persona2", 
            CreateTestConversationContext("persona2", "recent-session"));

        // Act
        await _memoryManager.CleanupOldMemoriesAsync(retention);

        // Assert
        _cacheMock.Verify(x => x.RemoveAsync(It.IsAny<string>(), default), Times.AtLeastOnce);
        _storeMock.Verify(x => x.CleanupOldDataAsync(It.IsAny<DateTime>()), Times.Once);
    }

    private ConversationContext CreateTestConversationContext(string personaId, string sessionId, int interactionCount = 1)
    {
        var context = new ConversationContext
        {
            PersonaId = personaId,
            SessionId = sessionId,
            StartTime = DateTime.UtcNow.AddHours(-1),
            LastInteraction = DateTime.UtcNow
        };

        for (int i = 0; i < interactionCount; i++)
        {
            context.InteractionHistory.Add(new InteractionSummary
            {
                UserInput = $"Test input {i}",
                PersonaResponse = $"Test response {i}",
                Intent = "test",
                WasSuccessful = true,
                Timestamp = DateTime.UtcNow.AddMinutes(-i * 5)
            });
        }

        return context;
    }
}