using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using DevOpsMcp.Domain.Interfaces;
using Eagle;
using Eagle._Components.Public;
using Microsoft.Extensions.Logging;

namespace DevOpsMcp.Infrastructure.Eagle;

/// <summary>
/// Converts Tcl dictionaries and lists to JSON format
/// </summary>
public sealed class TclDictionaryConverter : ITclDictionaryConverter
{
    private readonly ILogger<TclDictionaryConverter> _logger;
    
    private static readonly JsonSerializerOptions IndentedJsonOptions = new()
    {
        WriteIndented = true
    };
    
    public TclDictionaryConverter(ILogger<TclDictionaryConverter> logger)
    {
        _logger = logger;
    }
    
    /// <summary>
    /// Detects if a string is a Tcl dictionary format
    /// </summary>
    public bool IsTclDictionary(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
            
        var trimmed = value.Trim();
        
        // Check for "dict create" prefix
        if (trimmed.StartsWith("dict create", StringComparison.OrdinalIgnoreCase))
            return true;
            
        // Check for key-value pattern (simple heuristic)
        // Must have even number of whitespace-separated tokens
        var tokens = SplitTclTokens(trimmed);
        return tokens.Count > 0 && tokens.Count % 2 == 0 && !IsTclList(trimmed);
    }
    
    /// <summary>
    /// Detects if a string is a Tcl list format
    /// </summary>
    public bool IsTclList(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
            
        var trimmed = value.Trim();
        
        // Check for list command prefix
        if (trimmed.StartsWith("list ", StringComparison.OrdinalIgnoreCase))
            return true;
            
        // Check if it's enclosed in braces and contains nested structures
        if (trimmed.StartsWith('{') && trimmed.EndsWith('}'))
        {
            var inner = trimmed.Substring(1, trimmed.Length - 2);
            // If inner content has balanced braces, it's likely a list
            return HasBalancedBraces(inner);
        }
        
        return false;
    }
    
    /// <summary>
    /// Converts Tcl dictionary to JSON object
    /// </summary>
    public string ConvertTclDictToJson(string tclDict)
    {
        try
        {
            var dict = ParseTclDictionary(tclDict);
            return JsonSerializer.Serialize(dict, IndentedJsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to convert Tcl dictionary to JSON");
            // Return original string wrapped in JSON
            return JsonSerializer.Serialize(new { raw = tclDict });
        }
    }
    
    /// <summary>
    /// Converts Tcl list to JSON array
    /// </summary>
    public string ConvertTclListToJson(string tclList)
    {
        try
        {
            var list = ParseTclList(tclList);
            return JsonSerializer.Serialize(list, IndentedJsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to convert Tcl list to JSON");
            // Return original string wrapped in JSON
            return JsonSerializer.Serialize(new { raw = tclList });
        }
    }
    
    /// <summary>
    /// Parses a Tcl dictionary into a C# dictionary
    /// </summary>
    public Dictionary<string, object> ParseTclDictionary(string tclDict)
    {
        var result = new Dictionary<string, object>();
        var input = tclDict.Trim();
        
        // Remove "dict create" prefix if present
        if (input.StartsWith("dict create", StringComparison.OrdinalIgnoreCase))
        {
            input = input.Substring("dict create".Length).Trim();
        }
        
        var tokens = SplitTclTokens(input);
        
        for (int i = 0; i < tokens.Count - 1; i += 2)
        {
            var key = tokens[i];
            var value = tokens[i + 1];
            
            // Try to parse the value
            result[key] = ((ITclDictionaryConverter)this).ParseTclValue(value);
        }
        
        return result;
    }
    
    /// <summary>
    /// Parses a Tcl list into a C# list
    /// </summary>
    public List<object> ParseTclList(string tclList)
    {
        var result = new List<object>();
        var input = tclList.Trim();
        
        // Remove "list" prefix if present
        if (input.StartsWith("list ", StringComparison.OrdinalIgnoreCase))
        {
            input = input.Substring("list ".Length).Trim();
        }
        
        // Remove outer braces if present
        if (input.StartsWith('{') && input.EndsWith('}'))
        {
            input = input.Substring(1, input.Length - 2);
        }
        
        var tokens = SplitTclTokens(input);
        
        foreach (var token in tokens)
        {
            result.Add(((ITclDictionaryConverter)this).ParseTclValue(token));
        }
        
        return result;
    }
    
    /// <summary>
    /// Parses a Tcl value into appropriate C# type
    /// </summary>
    object ITclDictionaryConverter.ParseTclValue(string value)
    {
        // Remove quotes if present
        if ((value.StartsWith('"') && value.EndsWith('"')) ||
            (value.StartsWith('\'') && value.EndsWith('\'')))
        {
            return value.Substring(1, value.Length - 2);
        }
        
        // Check for nested dictionary
        if (value.StartsWith("[dict create", StringComparison.Ordinal) && value.EndsWith(']'))
        {
            var innerDict = value.Substring(1, value.Length - 2);
            return ParseTclDictionary(innerDict);
        }
        
        // Check for nested list
        if (value.StartsWith("[list", StringComparison.Ordinal) && value.EndsWith(']'))
        {
            var innerList = value.Substring(1, value.Length - 2);
            return ParseTclList(innerList);
        }
        
        // Check for braced content (could be dict or list)
        if (value.StartsWith('{') && value.EndsWith('}'))
        {
            var inner = value.Substring(1, value.Length - 2);
            
            // Try to determine if it's a dict or list
            var tokens = SplitTclTokens(inner);
            if (tokens.Count > 0 && tokens.Count % 2 == 0)
            {
                // Might be a dictionary
                try
                {
                    return ParseTclDictionary(inner);
                }
                catch
                {
                    // Fall through to list
                }
            }
            
            // Treat as list
            return ParseTclList(value);
        }
        
        // Try to parse as number
        if (int.TryParse(value, out int intValue))
        {
            return intValue;
        }
        
        if (double.TryParse(value, out double doubleValue))
        {
            return doubleValue;
        }
        
        // Check for boolean
        if (value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
            value == "1")
        {
            return true;
        }
        
        if (value.Equals("false", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("no", StringComparison.OrdinalIgnoreCase) ||
            value == "0")
        {
            return false;
        }
        
        // Return as string
        return value;
    }
    
    /// <summary>
    /// Splits Tcl tokens respecting braces and quotes
    /// </summary>
    private List<string> SplitTclTokens(string input)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();
        int braceDepth = 0;
        int bracketDepth = 0;
        bool inQuotes = false;
        char quoteChar = '\0';
        
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];
            
            if (!inQuotes)
            {
                if (c == '"' || c == '\'')
                {
                    inQuotes = true;
                    quoteChar = c;
                    current.Append(c);
                }
                else if (c == '{')
                {
                    braceDepth++;
                    current.Append(c);
                }
                else if (c == '}')
                {
                    braceDepth--;
                    current.Append(c);
                }
                else if (c == '[')
                {
                    bracketDepth++;
                    current.Append(c);
                }
                else if (c == ']')
                {
                    bracketDepth--;
                    current.Append(c);
                }
                else if (char.IsWhiteSpace(c) && braceDepth == 0 && bracketDepth == 0)
                {
                    if (current.Length > 0)
                    {
                        tokens.Add(current.ToString());
                        current.Clear();
                    }
                }
                else
                {
                    current.Append(c);
                }
            }
            else
            {
                current.Append(c);
                if (c == quoteChar && (i == 0 || input[i - 1] != '\\'))
                {
                    inQuotes = false;
                    quoteChar = '\0';
                }
            }
        }
        
        if (current.Length > 0)
        {
            tokens.Add(current.ToString());
        }
        
        return tokens;
    }
    
    /// <summary>
    /// Checks if a string has balanced braces
    /// </summary>
    private bool HasBalancedBraces(string input)
    {
        int depth = 0;
        foreach (char c in input)
        {
            if (c == '{') depth++;
            else if (c == '}') depth--;
            if (depth < 0) return false;
        }
        return depth == 0;
    }
    
    /// <summary>
    /// Converts a .NET dictionary to a Tcl dictionary string
    /// </summary>
    public string ToTclDictionary(Dictionary<string, object> dict)
    {
        var parts = new List<string>();
        foreach (var kvp in dict)
        {
            parts.Add(EscapeTclString(kvp.Key));
            parts.Add(EscapeTclString(kvp.Value?.ToString() ?? ""));
        }
        return string.Join(" ", parts);
    }
    
    /// <summary>
    /// Parses a Tcl list string into a .NET list of strings
    /// </summary>
    List<string> ITclDictionaryConverter.ParseTclList(string tclList)
    {
        var objects = ParseTclList(tclList);
        return objects.Select(o => o?.ToString() ?? "").ToList();
    }
    
    /// <summary>
    /// Converts a .NET list to a Tcl list string
    /// </summary>
    public string ToTclList(IEnumerable<string> list)
    {
        return string.Join(" ", list.Select(EscapeTclString));
    }
    
    private static string EscapeTclString(string str)
    {
        if (string.IsNullOrEmpty(str))
            return "{}";
        
        // If string contains special characters, wrap in braces
        if (str.Any(c => char.IsWhiteSpace(c) || c == '{' || c == '}' || c == '"' || c == '\\'))
        {
            // Escape braces within the string
            return "{" + str.Replace("{", "\\{").Replace("}", "\\}") + "}";
        }
        
        return str;
    }
}