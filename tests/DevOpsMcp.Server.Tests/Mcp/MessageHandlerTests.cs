using DevOpsMcp.Server.Mcp;
using DevOpsMcp.Server.Tools;
using Xunit;
using FluentAssertions;
using Moq;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DevOpsMcp.Server.Tests.Mcp;

public class MessageHandlerTests
{
    private readonly Mock<IToolRegistry> _toolRegistryMock;
    private readonly Mock<ILogger<MessageHandler>> _loggerMock;
    private readonly MessageHandler _handler;

    public MessageHandlerTests()
    {
        _toolRegistryMock = new Mock<IToolRegistry>();
        _loggerMock = new Mock<ILogger<MessageHandler>>();
        _handler = new MessageHandler(_toolRegistryMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task HandleRequestAsync_Initialize_ReturnsInitializeResponse()
    {
        // Arrange
        var initRequest = new InitializeRequest
        {
            ProtocolVersion = "2024-11-05",
            Capabilities = new ClientCapabilities(),
            ClientInfo = new ClientInfo { Name = "Test Client", Version = "1.0.0" }
        };

        var request = new McpRequest
        {
            Jsonrpc = "2.0",
            Method = "initialize",
            Params = JsonSerializer.SerializeToElement(initRequest),
            Id = 1
        };

        // Act
        var response = await _handler.HandleRequestAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Error.Should().BeNull();
        response.Id.Should().Be(1);
        
        var result = JsonSerializer.Deserialize<InitializeResponse>(
            JsonSerializer.Serialize(response.Result));
        
        result.Should().NotBeNull();
        result!.ProtocolVersion.Should().Be("2024-11-05");
        result.ServerInfo.Name.Should().Be("DevOps MCP Server");
        result.ServerInfo.Version.Should().Be("1.0.0");
        result.Capabilities.Tools.Should().NotBeNull();
        result.Capabilities.Logging.Should().NotBeNull();
    }

    [Fact]
    public async Task HandleRequestAsync_ToolsList_ReturnsTools()
    {
        // Arrange
        var tools = new List<Tool>
        {
            new() { Name = "test_tool", Description = "Test tool", InputSchema = JsonDocument.Parse("{}").RootElement }
        };

        _toolRegistryMock.Setup(x => x.GetToolsAsync()).ReturnsAsync(tools);

        var request = new McpRequest
        {
            Jsonrpc = "2.0",
            Method = "tools/list",
            Id = 2
        };

        // Act
        var response = await _handler.HandleRequestAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Error.Should().BeNull();
        
        var result = JsonSerializer.Deserialize<ListToolsResponse>(
            JsonSerializer.Serialize(response.Result));
        
        result.Should().NotBeNull();
        result!.Tools.Should().HaveCount(1);
        result.Tools[0].Name.Should().Be("test_tool");
    }

    [Fact]
    public async Task HandleRequestAsync_ToolsCall_CallsTool()
    {
        // Arrange
        var callRequest = new CallToolRequest
        {
            Name = "test_tool",
            Arguments = JsonSerializer.SerializeToElement(new { param = "value" })
        };

        var expectedResponse = new CallToolResponse
        {
            Content = new List<ToolContent>
            {
                new() { Type = "text", Text = "Tool executed successfully" }
            },
            IsError = false
        };

        _toolRegistryMock
            .Setup(x => x.CallToolAsync(
                It.IsAny<string>(), 
                It.IsAny<JsonElement?>(), 
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        var request = new McpRequest
        {
            Jsonrpc = "2.0",
            Method = "tools/call",
            Params = JsonSerializer.SerializeToElement(callRequest),
            Id = 3
        };

        // Act
        var response = await _handler.HandleRequestAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Error.Should().BeNull();
        
        _toolRegistryMock.Verify(
            x => x.CallToolAsync("test_tool", It.IsAny<JsonElement?>(), It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task HandleRequestAsync_UnknownMethod_ReturnsMethodNotFound()
    {
        // Arrange
        var request = new McpRequest
        {
            Jsonrpc = "2.0",
            Method = "unknown/method",
            Id = 4
        };

        // Act
        var response = await _handler.HandleRequestAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Error.Should().NotBeNull();
        response.Error!.Code.Should().Be(-32601);
        response.Error.Message.Should().Be("Method not found");
    }

    [Fact]
    public async Task HandleRequestAsync_Ping_ReturnsPong()
    {
        // Arrange
        var request = new McpRequest
        {
            Jsonrpc = "2.0",
            Method = "ping",
            Id = 5
        };

        // Act
        var response = await _handler.HandleRequestAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Error.Should().BeNull();
        
        var result = JsonSerializer.Deserialize<JsonElement>(
            JsonSerializer.Serialize(response.Result));
        
        result.GetProperty("pong").GetBoolean().Should().BeTrue();
        result.TryGetProperty("timestamp", out _).Should().BeTrue();
    }

    [Fact]
    public async Task HandleNotificationAsync_Cancelled_LogsInformation()
    {
        // Arrange
        var notification = new McpNotification
        {
            Jsonrpc = "2.0",
            Method = "cancelled",
            Params = JsonSerializer.SerializeToElement(new { requestId = "123" })
        };

        // Act
        await _handler.HandleNotificationAsync(notification);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("123")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}