using DevOpsMcp.Server.Mcp;

namespace DevOpsMcp.Server.Tools;

public abstract class BaseTool<TArguments> : ITool where TArguments : class
{
    private readonly JsonSerializerOptions _jsonOptions;
    
    protected BaseTool()
    {
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract JsonElement InputSchema { get; }

    public async Task<CallToolResponse> ExecuteAsync(JsonElement? arguments, CancellationToken cancellationToken = default)
    {
        try
        {
            TArguments? args = null;
            if (arguments.HasValue)
            {
                args = arguments.Value.Deserialize<TArguments>(_jsonOptions);
                if (args == null)
                {
                    return CreateErrorResponse("Invalid arguments provided");
                }
            }

            return await ExecuteInternalAsync(args!, cancellationToken);
        }
        catch (JsonException ex)
        {
            return CreateErrorResponse($"Failed to parse arguments: {ex.Message}");
        }
        catch (Exception ex)
        {
            return CreateErrorResponse($"Tool execution failed: {ex.Message}");
        }
    }

    protected abstract Task<CallToolResponse> ExecuteInternalAsync(TArguments arguments, CancellationToken cancellationToken);

    protected CallToolResponse CreateSuccessResponse(string text)
    {
        return new CallToolResponse
        {
            Content = new List<ToolContent>
            {
                new()
                {
                    Type = "text",
                    Text = text
                }
            },
            IsError = false
        };
    }

    protected CallToolResponse CreateJsonResponse(object data)
    {
        var json = JsonSerializer.Serialize(data, _jsonOptions);
        return new CallToolResponse
        {
            Content = new List<ToolContent>
            {
                new()
                {
                    Type = "text",
                    Text = json
                }
            },
            IsError = false
        };
    }

    protected CallToolResponse CreateErrorResponse(string error)
    {
        return new CallToolResponse
        {
            Content = new List<ToolContent>
            {
                new()
                {
                    Type = "text",
                    Text = error
                }
            },
            IsError = true
        };
    }

    protected static JsonElement CreateSchema<T>()
    {
        var schemaJson = JsonSchemaGenerator.Generate<T>();
        return JsonSerializer.Deserialize<JsonElement>(schemaJson);
    }
}

internal static class JsonSchemaGenerator
{
    public static string Generate<T>()
    {
        var type = typeof(T);
        var properties = new Dictionary<string, object>();
        var required = new List<string>();

        foreach (var prop in type.GetProperties())
        {
            var propSchema = new Dictionary<string, object>
            {
                ["type"] = GetJsonType(prop.PropertyType),
                ["description"] = prop.Name
            };

            properties[JsonNamingPolicy.CamelCase.ConvertName(prop.Name)] = propSchema;

            if (!IsNullable(prop.PropertyType))
            {
                required.Add(JsonNamingPolicy.CamelCase.ConvertName(prop.Name));
            }
        }

        var schema = new
        {
            type = "object",
            properties,
            required
        };

        return JsonSerializer.Serialize(schema);
    }

    private static string GetJsonType(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

        if (underlyingType == typeof(string))
            return "string";
        if (underlyingType == typeof(int) || underlyingType == typeof(long))
            return "integer";
        if (underlyingType == typeof(double) || underlyingType == typeof(float) || underlyingType == typeof(decimal))
            return "number";
        if (underlyingType == typeof(bool))
            return "boolean";
        if (underlyingType.IsArray || typeof(System.Collections.IEnumerable).IsAssignableFrom(underlyingType))
            return "array";
        
        return "object";
    }

    private static bool IsNullable(Type type)
    {
        return Nullable.GetUnderlyingType(type) != null || !type.IsValueType;
    }
}