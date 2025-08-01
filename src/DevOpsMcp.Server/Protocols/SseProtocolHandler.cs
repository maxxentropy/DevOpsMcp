using DevOpsMcp.Server.Mcp;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace DevOpsMcp.Server.Protocols;

public sealed class SseProtocolHandler(
    IMessageHandler messageHandler,
    IConnectionManager connectionManager,
    ILogger<SseProtocolHandler> logger)
    : IProtocolHandler
{
    private readonly McpJsonSerializerContext _jsonContext = new();

    public string Name => "sse";

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("SSE protocol handler started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("SSE protocol handler stopped");
        return Task.CompletedTask;
    }

    public async Task HandleConnectionAsync(HttpContext context)
    {
        var connectionId = Guid.NewGuid().ToString();
        logger.LogInformation("New SSE connection: {ConnectionId}", connectionId);

        context.Response.Headers.Append("Content-Type", "text/event-stream");
        context.Response.Headers.Append("Cache-Control", "no-cache");
        context.Response.Headers.Append("Connection", "keep-alive");
        context.Response.Headers.Append("X-Accel-Buffering", "no");

        var sender = new SseMessageSender(context.Response, _jsonContext, logger);
        await connectionManager.AddConnectionAsync(connectionId, sender);

        try
        {
            await context.Response.Body.FlushAsync();
            
            // Send initial ping
            await sender.SendEventAsync("ping", new PingEvent { Timestamp = DateTimeOffset.UtcNow }, context.RequestAborted);

            // Keep connection alive
            while (!context.RequestAborted.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(30), context.RequestAborted);
                await sender.SendEventAsync("ping", new PingEvent { Timestamp = DateTimeOffset.UtcNow }, context.RequestAborted);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("SSE connection {ConnectionId} cancelled", connectionId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in SSE connection {ConnectionId}", connectionId);
        }
        finally
        {
            await connectionManager.RemoveConnectionAsync(connectionId);
            logger.LogInformation("SSE connection {ConnectionId} closed", connectionId);
        }
    }

    public async Task<IResult> HandleRequestAsync(HttpContext context, McpRequest request)
    {
        try
        {
            var response = await messageHandler.HandleRequestAsync(request, context.RequestAborted);
            var json = JsonSerializer.Serialize(response, _jsonContext.McpResponse);
            return Results.Content(json, "application/json");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling SSE request");
            var errorResponse = new McpResponse
            {
                Jsonrpc = "2.0",
                Error = new McpError
                {
                    Code = -32603,
                    Message = "Internal error",
                    Data = ex.Message
                },
                Id = request.Id ?? null!
            };
            var errorJson = JsonSerializer.Serialize(errorResponse, _jsonContext.McpResponse);
            return Results.Content(errorJson, "application/json", statusCode: 500);
        }
    }
}

internal sealed class SseMessageSender(
    HttpResponse response,
    McpJsonSerializerContext jsonContext,
    Microsoft.Extensions.Logging.ILogger logger)
    : IMessageSender
{
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public async Task SendResponseAsync(McpResponse response, CancellationToken cancellationToken = default)
    {
        await SendEventAsync("message", response, cancellationToken);
    }

    public async Task SendNotificationAsync(McpNotification notification, CancellationToken cancellationToken = default)
    {
        await SendEventAsync("notification", notification, cancellationToken);
    }

    public async Task SendEventAsync(string eventType, object data, CancellationToken cancellationToken = default)
    {
        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            string json;
            if (data is McpResponse response1)
                json = JsonSerializer.Serialize(response1, jsonContext.McpResponse);
            else if (data is McpNotification notification)
                json = JsonSerializer.Serialize(notification, jsonContext.McpNotification);
            else if (data is PingEvent ping)
                json = JsonSerializer.Serialize(ping, jsonContext.PingEvent);
            else
                json = JsonSerializer.Serialize(data, jsonContext.Object);
                
            var message = $"event: {eventType}\ndata: {json}\n\n";
            
            await response.WriteAsync(message, cancellationToken);
            await response.Body.FlushAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error sending SSE message");
            throw;
        }
        finally
        {
            _writeLock.Release();
        }
    }
}