using System.Text.Json;
using DevOpsMcp.Server.Mcp;

namespace DevOpsMcp.Server.Tests.Tools.Email;

public static class EmailToolTestHelpers
{
    public static string GetResponseContent(CallToolResponse response)
    {
        return response.Content.FirstOrDefault()?.Text ?? string.Empty;
    }
    
    public static T? DeserializeResponse<T>(CallToolResponse response)
    {
        var content = GetResponseContent(response);
        return string.IsNullOrEmpty(content) ? default : JsonSerializer.Deserialize<T>(content);
    }
    
    public static JsonElement DeserializeResponseAsJsonElement(CallToolResponse response)
    {
        var content = GetResponseContent(response);
        return string.IsNullOrEmpty(content) 
            ? default 
            : JsonSerializer.Deserialize<JsonElement>(content);
    }
    
    public static Dictionary<string, JsonElement> DeserializeResponseAsDictionary(CallToolResponse response)
    {
        var content = GetResponseContent(response);
        return string.IsNullOrEmpty(content) 
            ? new Dictionary<string, JsonElement>() 
            : JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(content) ?? new Dictionary<string, JsonElement>();
    }
}