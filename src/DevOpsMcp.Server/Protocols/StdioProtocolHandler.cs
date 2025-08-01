using System.IO;
using DevOpsMcp.Server.Mcp;
using System.Text.Json;
using System.Text;

namespace DevOpsMcp.Server.Protocols;

public sealed class StdioProtocolHandler(
    IMessageHandler messageHandler,
    ILogger<StdioProtocolHandler> logger)
    : IProtocolHandler, IMessageSender
{
    private readonly McpJsonSerializerContext _jsonContext = new();
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _readTask;

    public string Name => "stdio";

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Starting stdio protocol handler");
        
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _readTask = ReadLoopAsync(_cancellationTokenSource.Token);
        
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Stopping stdio protocol handler");
        
        await (_cancellationTokenSource?.CancelAsync() ?? Task.CompletedTask);
        
        if (_readTask != null)
        {
            try
            {
                await _readTask.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
            }
            catch (TimeoutException)
            {
                logger.LogWarning("Read loop did not complete within timeout");
            }
        }
        
        _cancellationTokenSource?.Dispose();
    }

    public async Task SendResponseAsync(McpResponse response, CancellationToken cancellationToken = default)
    {
        await WriteMessageAsync(response, cancellationToken);
    }

    public async Task SendNotificationAsync(McpNotification notification, CancellationToken cancellationToken = default)
    {
        await WriteMessageAsync(notification, cancellationToken);
    }

    private async Task ReadLoopAsync(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Console.OpenStandardInput(), Encoding.UTF8);
        var buffer = new StringBuilder();
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                
                if (line == null)
                {
                    logger.LogInformation("Stdin closed, shutting down");
                    break;
                }
                
                if (string.IsNullOrWhiteSpace(line))
                {
                    if (buffer.Length > 0)
                    {
                        var json = buffer.ToString();
                        buffer.Clear();
                        
                        _ = Task.Run(async () => await ProcessMessageAsync(json, cancellationToken), cancellationToken);
                    }
                }
                else
                {
                    buffer.AppendLine(line);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error reading from stdin");
            }
        }
    }

    private async Task ProcessMessageAsync(string json, CancellationToken cancellationToken)
    {
        try
        {
            var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            
            if (root.TryGetProperty("method", out _))
            {
                if (root.TryGetProperty("id", out _))
                {
                    var request = JsonSerializer.Deserialize(json, _jsonContext.McpRequest);
                    if (request != null)
                    {
                        var response = await messageHandler.HandleRequestAsync(request, cancellationToken);
                        await SendResponseAsync(response, cancellationToken);
                    }
                }
                else
                {
                    var notification = JsonSerializer.Deserialize(json, _jsonContext.McpNotification);
                    if (notification != null)
                    {
                        await messageHandler.HandleNotificationAsync(notification, cancellationToken);
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Invalid JSON received");
            
            var errorResponse = new McpResponse
            {
                Jsonrpc = "2.0",
                Error = new McpError
                {
                    Code = -32700,
                    Message = "Parse error"
                },
                Id = null!
            };
            
            await SendResponseAsync(errorResponse, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing message");
        }
    }

    private async Task WriteMessageAsync(object message, CancellationToken cancellationToken)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            string json;
            if (message is McpResponse response)
                json = JsonSerializer.Serialize(response, _jsonContext.McpResponse);
            else if (message is McpNotification notification)
                json = JsonSerializer.Serialize(notification, _jsonContext.McpNotification);
            else
                json = JsonSerializer.Serialize(message, _jsonContext.Object);
                
            await Console.Out.WriteLineAsync(json.AsMemory(), cancellationToken);
            await Console.Out.WriteLineAsync();
            await Console.Out.FlushAsync(cancellationToken);
        }
        finally
        {
            _writeLock.Release();
        }
    }
}