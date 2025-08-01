using System.IO;
using DevOpsMcp.Server.Mcp;

namespace DevOpsMcp.Server.Protocols;

public sealed class StdioProtocolHandler : IProtocolHandler, IMessageSender
{
    private readonly IMessageHandler _messageHandler;
    private readonly ILogger<StdioProtocolHandler> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _readTask;

    public string Name => "stdio";

    public StdioProtocolHandler(
        IMessageHandler messageHandler,
        ILogger<StdioProtocolHandler> logger)
    {
        _messageHandler = messageHandler;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting stdio protocol handler");
        
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _readTask = ReadLoopAsync(_cancellationTokenSource.Token);
        
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping stdio protocol handler");
        
        _cancellationTokenSource?.Cancel();
        
        if (_readTask != null)
        {
            try
            {
                await _readTask.WaitAsync(TimeSpan.FromSeconds(5), cancellationToken);
            }
            catch (TimeoutException)
            {
                _logger.LogWarning("Read loop did not complete within timeout");
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
                    _logger.LogInformation("Stdin closed, shutting down");
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
                _logger.LogError(ex, "Error reading from stdin");
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
                    var request = JsonSerializer.Deserialize<McpRequest>(json, _jsonOptions);
                    if (request != null)
                    {
                        var response = await _messageHandler.HandleRequestAsync(request, cancellationToken);
                        await SendResponseAsync(response, cancellationToken);
                    }
                }
                else
                {
                    var notification = JsonSerializer.Deserialize<McpNotification>(json, _jsonOptions);
                    if (notification != null)
                    {
                        await _messageHandler.HandleNotificationAsync(notification, cancellationToken);
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON received");
            
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
            _logger.LogError(ex, "Error processing message");
        }
    }

    private async Task WriteMessageAsync(object message, CancellationToken cancellationToken)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            var json = JsonSerializer.Serialize(message, _jsonOptions);
            await Console.Out.WriteLineAsync(json.AsMemory(), cancellationToken);
            await Console.Out.WriteLineAsync();
            await Console.Out.FlushAsync();
        }
        finally
        {
            _writeLock.Release();
        }
    }
}