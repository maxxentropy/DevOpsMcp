using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using DevOpsMcp.Server.Mcp;

namespace DevOpsMcp.Server.Protocols;

/// <summary>
/// Handles the Streamable HTTP transport protocol for MCP
/// Supports both request-response and streaming modes
/// </summary>
public class StreamableHttpHandler
{
    private readonly IMessageHandler _messageHandler;
    private readonly IConnectionManager _connectionManager;
    private readonly ILogger<StreamableHttpHandler> _logger;
    private readonly McpJsonSerializerContext _jsonContext;
    private readonly JsonSerializerOptions _jsonOptions;

    public StreamableHttpHandler(
        IMessageHandler messageHandler,
        IConnectionManager connectionManager,
        ILogger<StreamableHttpHandler> logger)
    {
        _messageHandler = messageHandler;
        _connectionManager = connectionManager;
        _logger = logger;
        _jsonContext = new McpJsonSerializerContext();
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <summary>
    /// Handle incoming HTTP requests for the MCP endpoint
    /// Supports both POST and GET methods
    /// </summary>
    public async Task HandleRequestAsync(HttpContext context)
    {
        try
        {
            // Handle CORS preflight
            if (context.Request.Method == HttpMethods.Options)
            {
                await HandleCorsPreflightAsync(context);
                return;
            }

            // Extract session ID if present
            var sessionId = context.Request.Headers["Mcp-Session-Id"].FirstOrDefault() 
                ?? Guid.NewGuid().ToString();

            // Handle based on method
            if (context.Request.Method == HttpMethods.Get)
            {
                await HandleGetRequestAsync(context, sessionId);
            }
            else if (context.Request.Method == HttpMethods.Post)
            {
                await HandlePostRequestAsync(context, sessionId);
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
                await context.Response.WriteAsync("Only GET and POST methods are supported");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling HTTP request");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            var errorResponse = new McpResponse
            {
                Jsonrpc = "2.0",
                Error = new McpError
                {
                    Code = -32603,
                    Message = "Internal error",
                    Data = ex.Message
                },
                Id = 0
            };
            await WriteJsonResponseAsync(context, errorResponse);
        }
    }

    private async Task HandleGetRequestAsync(HttpContext context, string sessionId)
    {
        // GET requests are used for SSE streaming
        context.Response.ContentType = "text/event-stream";
        context.Response.Headers.Append("Cache-Control", "no-cache");
        context.Response.Headers.Append("Connection", "keep-alive");
        context.Response.Headers.Append("X-Accel-Buffering", "no");

        // Add session ID to response
        context.Response.Headers.Append("Mcp-Session-Id", sessionId);

        await context.Response.Body.FlushAsync();

        // Keep connection alive and handle server-sent events
        var connection = await _connectionManager.CreateSessionConnectionAsync(sessionId);
        
        try
        {
            while (!context.RequestAborted.IsCancellationRequested)
            {
                // Wait for messages to send
                var message = await connection.DequeueMessageAsync(context.RequestAborted);
                if (message != null)
                {
                    await SendServerSentEventAsync(context, message);
                }
            }
        }
        finally
        {
            await _connectionManager.RemoveConnectionAsync(sessionId);
        }
    }

    private async Task HandlePostRequestAsync(HttpContext context, string sessionId)
    {
        // Read the request body
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
        var requestBody = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(requestBody))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var errorResponse = new McpResponse
            {
                Jsonrpc = "2.0",
                Error = new McpError
                {
                    Code = -32700,
                    Message = "Parse error"
                },
                Id = 0
            };
            await WriteJsonResponseAsync(context, errorResponse);
            return;
        }

        try
        {
            var request = JsonSerializer.Deserialize(requestBody, _jsonContext.McpRequest);
            if (request == null)
            {
                throw new JsonException("Invalid request format");
            }

            // Process the request
            var response = await _messageHandler.ProcessRequestAsync(request);

            // Check if client accepts streaming
            var acceptsStreaming = context.Request.Headers.Accept
                .Any(h => h?.Contains("text/event-stream") == true);

            // Determine if streaming is required based on the request method or response type
            var requiresStreaming = request.Method == "tools/call" || 
                                    request.Method?.StartsWith("prompts/", StringComparison.Ordinal) == true ||
                                    request.Method?.StartsWith("resources/", StringComparison.Ordinal) == true;

            if (acceptsStreaming && requiresStreaming)
            {
                // Switch to SSE for streaming response
                context.Response.ContentType = "text/event-stream";
                context.Response.Headers.Append("Cache-Control", "no-cache");
                context.Response.Headers.Append("Mcp-Session-Id", sessionId);

                await SendServerSentEventAsync(context, response);
                
                // Send additional streamed messages if any
                var connection = await _connectionManager.GetSessionConnectionAsync(sessionId);
                if (connection != null)
                {
                    while (connection.HasPendingMessages)
                    {
                        var message = await connection.DequeueMessageAsync(context.RequestAborted);
                        if (message != null)
                        {
                            await SendServerSentEventAsync(context, message);
                        }
                    }
                }
            }
            else
            {
                // Send regular JSON response
                context.Response.Headers.Append("Mcp-Session-Id", sessionId);
                await WriteJsonResponseAsync(context, response);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Invalid JSON in request");
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            var errorResponse = new McpResponse
            {
                Jsonrpc = "2.0",
                Error = new McpError
                {
                    Code = -32700,
                    Message = "Parse error",
                    Data = ex.Message
                },
                Id = 0
            };
            await WriteJsonResponseAsync(context, errorResponse);
        }
    }

    private async Task SendServerSentEventAsync(HttpContext context, object data)
    {
        var json = JsonSerializer.Serialize(data, typeof(object), _jsonContext);
        var eventData = $"data: {json}\n\n";
        var bytes = Encoding.UTF8.GetBytes(eventData);
        
        await context.Response.Body.WriteAsync(bytes);
        await context.Response.Body.FlushAsync();
    }

    private async Task WriteJsonResponseAsync(HttpContext context, object response)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        string json;
        
        if (response is McpResponse mcpResponse)
        {
            json = JsonSerializer.Serialize(mcpResponse, _jsonContext.McpResponse);
        }
        else
        {
            // Fallback to regular JsonSerializer for other types
            json = JsonSerializer.Serialize(response, _jsonOptions);
        }
        
        await context.Response.WriteAsync(json);
    }

    private async Task HandleCorsPreflightAsync(HttpContext context)
    {
        context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
        context.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
        context.Response.Headers.Append("Access-Control-Allow-Headers", "Content-Type, Accept, Mcp-Session-Id, Authorization");
        context.Response.Headers.Append("Access-Control-Max-Age", "86400");
        context.Response.StatusCode = StatusCodes.Status204NoContent;
        await Task.CompletedTask;
    }
}

/// <summary>
/// Extension methods for registering the Streamable HTTP handler
/// </summary>
public static class StreamableHttpExtensions
{
    public static WebApplication MapMcp(this WebApplication app, string path = "/mcp")
    {
        // Map the unified MCP endpoint that handles both GET and POST
        app.Map(path, async (HttpContext context, StreamableHttpHandler handler) =>
        {
            await handler.HandleRequestAsync(context);
        });

        // Add CORS middleware for the MCP endpoints
        app.UseCors(builder => builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("Mcp-Session-Id"));

        return app;
    }
}