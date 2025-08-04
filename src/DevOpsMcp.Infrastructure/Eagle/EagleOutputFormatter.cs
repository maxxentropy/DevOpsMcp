using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using DevOpsMcp.Domain.Eagle;
using DevOpsMcp.Domain.Interfaces;
using Eagle;
using Eagle._Components.Public;
using Microsoft.Extensions.Logging;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DevOpsMcp.Infrastructure.Eagle;

/// <summary>
/// Provides structured output formatting capabilities for Eagle scripts
/// Part of Phase 1.2: Structured Output Processing
/// </summary>
public class EagleOutputFormatter : IEagleOutputFormatter
{
    private readonly ILogger<EagleOutputFormatter> _logger;
    private readonly ITclDictionaryConverter _tclConverter;
    private readonly ISerializer _yamlSerializer;
    private readonly IDeserializer _yamlDeserializer;
    private readonly JsonSerializerOptions _jsonOptions;

    public EagleOutputFormatter(
        ILogger<EagleOutputFormatter> logger,
        ITclDictionaryConverter tclConverter)
    {
        _logger = logger;
        _tclConverter = tclConverter;
        
        // Initialize YAML serializer with camelCase naming
        _yamlSerializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
            
        _yamlDeserializer = new DeserializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
            
        // Initialize JSON options
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };
    }

    /// <summary>
    /// Injects output formatting commands into an Eagle interpreter
    /// </summary>
    public void InjectOutputCommands(Interpreter interpreter)
    {
        // This overload exists for direct Eagle Interpreter usage
        InjectOutputCommandsInternal(interpreter);
    }
    
    /// <summary>
    /// Injects output formatting commands into a script interpreter
    /// </summary>
    /// <param name="interpreter">The script interpreter abstraction</param>
    /// <remarks>
    /// This method is part of the IEagleOutputFormatter interface to maintain clean architecture.
    /// The Domain layer uses IScriptInterpreter abstraction to avoid depending on Eagle-specific types.
    /// This follows the same pattern as IEagleContextProvider.InjectRichContext.
    /// 
    /// The structured output processing feature (Phase 1.2) requires injecting commands like:
    /// - output::json - for JSON formatting
    /// - output::xml - for XML formatting  
    /// - output::yaml - for YAML formatting
    /// - output::table - for table formatting
    /// These commands are essential for the StructuredOutput.test.tcl test script to pass.
    /// </remarks>
    public void InjectOutputCommands(IScriptInterpreter interpreter)
    {
        // Extract the Eagle Interpreter from the abstraction
        // This is safe because we control the implementation of IScriptInterpreter
        if (interpreter is EagleInterpreterAdapter adapter)
        {
            InjectOutputCommandsInternal(adapter.UnderlyingInterpreter);
        }
        else
        {
            throw new ArgumentException($"Expected EagleInterpreterAdapter but got {interpreter.GetType().Name}", nameof(interpreter));
        }
    }
    
    private void InjectOutputCommandsInternal(Interpreter interpreter)
    {
        try
        {
            // Create output::json command
            var jsonScript = @"
namespace eval output {
    proc json {data} {
        # Convert Tcl data structure to JSON
        # This is a simplified implementation
        if {[llength $data] == 1} {
            return ""\""$data\""""
        } else {
            set result ""[""
            set first 1
            foreach item $data {
                if {!$first} {
                    append result "",""
                }
                append result ""\""$item\""""
                set first 0
            }
            append result ""]""
            return $result
        }
    }
    
    proc json_object {args} {
        # Create a JSON object from key-value pairs
        set result ""{""
        set first 1
        foreach {key value} $args {
            if {!$first} {
                append result "",""
            }
            append result ""\""$key\"": ""
            if {[string is integer -strict $value] || [string is double -strict $value]} {
                append result $value
            } elseif {$value eq ""true"" || $value eq ""false"" || $value eq ""null""} {
                append result $value
            } else {
                append result ""\""$value\""""
            }
            set first 0
        }
        append result ""}""
        return $result
    }
}";

            Result? result = null;
            var code = interpreter.EvaluateScript(jsonScript, ref result);
            if (code != ReturnCode.Ok)
            {
                _logger.LogError("Failed to create output::json commands: {Error}", result);
            }

            // Create output::xml command
            var xmlScript = @"
namespace eval output {
    proc xml {tag content {attributes """"}} {
        set result ""<$tag""
        if {$attributes ne """"} {
            foreach {key value} $attributes {
                append result "" $key=\""$value\""""
            }
        }
        if {$content eq """"} {
            append result ""/>""
        } else {
            append result "">$content</$tag>""
        }
        return $result
    }
    
    proc xml_element {tag children {attributes """"}} {
        set result ""<$tag""
        if {$attributes ne """"} {
            foreach {key value} $attributes {
                append result "" $key=\""$value\""""
            }
        }
        append result "">""
        foreach child $children {
            append result $child
        }
        append result ""</$tag>""
        return $result
    }
}";

            code = interpreter.EvaluateScript(xmlScript, ref result);
            if (code != ReturnCode.Ok)
            {
                _logger.LogError("Failed to create output::xml commands: {Error}", result);
            }

            // Create output::yaml command
            var yamlScript = @"
namespace eval output {
    proc yaml {data {indent 0}} {
        # Convert Tcl data to YAML-like format
        set spaces [string repeat "" "" $indent]
        if {[llength $data] == 1} {
            return ""${spaces}- $data""
        } else {
            set result """"
            foreach item $data {
                append result ""${spaces}- $item\n""
            }
            return [string trimright $result]
        }
    }
    
    proc yaml_map {args {indent 0}} {
        # Create YAML map from key-value pairs
        set spaces [string repeat "" "" $indent]
        set result """"
        foreach {key value} $args {
            append result ""${spaces}$key: $value\n""
        }
        return [string trimright $result]
    }
}";

            code = interpreter.EvaluateScript(yamlScript, ref result);
            if (code != ReturnCode.Ok)
            {
                _logger.LogError("Failed to create output::yaml commands: {Error}", result);
            }

            // Create output::table command
            var tableScript = @"
namespace eval output {
    proc table {headers rows} {
        # Create formatted table output
        set col_widths {}
        
        # Calculate column widths
        foreach header $headers {
            lappend col_widths [string length $header]
        }
        
        foreach row $rows {
            set i 0
            foreach cell $row {
                set width [string length $cell]
                if {$width > [lindex $col_widths $i]} {
                    lset col_widths $i $width
                }
                incr i
            }
        }
        
        # Build separator
        set separator ""+""
        foreach width $col_widths {
            append separator [string repeat ""-"" [expr {$width + 2}]]
            append separator ""+""
        }
        
        # Build header row
        set result $separator
        append result ""\n|""
        set i 0
        foreach header $headers {
            set width [lindex $col_widths $i]
            append result [format "" %-${width}s |"" $header]
            incr i
        }
        append result ""\n$separator""
        
        # Build data rows
        foreach row $rows {
            append result ""\n|""
            set i 0
            foreach cell $row {
                set width [lindex $col_widths $i]
                append result [format "" %-${width}s |"" $cell]
                incr i
            }
        }
        append result ""\n$separator""
        
        return $result
    }
}";

            code = interpreter.EvaluateScript(tableScript, ref result);
            if (code != ReturnCode.Ok)
            {
                _logger.LogError("Failed to create output::table command: {Error}", result);
            }

            // Create output::error command for standardized error formatting
            var errorScript = @"
namespace eval output {
    proc error {code message {details """"}} {
        set error_obj [output::json_object \
            error true \
            code $code \
            message $message]
        
        if {$details ne """"} {
            set error_obj [string trimright $error_obj ""}""]
            append error_obj "", \""details\"": \""$details\""}""
        }
        
        return $error_obj
    }
    
    proc success {data {message """"}} {
        if {$message eq """"} {
            return [output::json_object \
                success true \
                data $data]
        } else {
            return [output::json_object \
                success true \
                message $message \
                data $data]
        }
    }
}";

            code = interpreter.EvaluateScript(errorScript, ref result);
            if (code != ReturnCode.Ok)
            {
                _logger.LogError("Failed to create output::error command: {Error}", result);
            }

            _logger.LogDebug("Injected structured output commands into Eagle interpreter");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to inject output commands into Eagle interpreter");
            throw;
        }
    }

    /// <summary>
    /// Processes raw output to detect and convert structured data
    /// </summary>
    public object ProcessOutput(string rawOutput)
    {
        if (string.IsNullOrWhiteSpace(rawOutput))
            return new { content = "", type = "text" };
            
        var trimmed = rawOutput.Trim();
        
        // Detect Tcl dictionary format
        if (_tclConverter.IsTclDictionary(trimmed))
        {
            try
            {
                var dict = _tclConverter.ParseTclDictionary(trimmed);
                return dict;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse Tcl dictionary");
            }
        }
        
        // Detect Tcl list format
        if (_tclConverter.IsTclList(trimmed))
        {
            try
            {
                var list = _tclConverter.ParseTclList(trimmed);
                return list;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse Tcl list");
            }
        }
        
        // Detect JSON format
        if ((trimmed.StartsWith('{') && trimmed.EndsWith('}')) ||
            (trimmed.StartsWith('[') && trimmed.EndsWith(']')))
        {
            try
            {
                return JsonSerializer.Deserialize<object>(trimmed) ?? new { content = rawOutput, type = "text" };
            }
            catch
            {
                // Not valid JSON, treat as text
            }
        }
        
        // Return as text content
        return new { content = rawOutput, type = "text" };
    }

    /// <summary>
    /// Formats Eagle script output based on the requested format
    /// </summary>
    public FormattedOutput Format(string rawOutput, OutputFormat format)
    {
        try
        {
            switch (format)
            {
                case OutputFormat.Json:
                    return FormatAsJson(rawOutput);
                    
                case OutputFormat.Xml:
                    return FormatAsXml(rawOutput);
                    
                case OutputFormat.Yaml:
                    return FormatAsYaml(rawOutput);
                    
                case OutputFormat.Table:
                    return FormatAsTable(rawOutput);
                    
                case OutputFormat.Csv:
                    return FormatAsCsv(rawOutput);
                    
                case OutputFormat.Markdown:
                    return FormatAsMarkdown(rawOutput);
                    
                case OutputFormat.Plain:
                default:
                    return new FormattedOutput
                    {
                        Format = OutputFormat.Plain,
                        Content = rawOutput,
                        ContentType = "text/plain"
                    };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to format output as {Format}", format);
            return new FormattedOutput
            {
                Format = format,
                Content = rawOutput,
                ContentType = "text/plain",
                Error = $"Failed to format as {format}: {ex.Message}"
            };
        }
    }

    private FormattedOutput FormatAsJson(string rawOutput)
    {
        // Try to parse as JSON first
        try
        {
            using var json = JsonDocument.Parse(rawOutput);
            return new FormattedOutput
            {
                Format = OutputFormat.Json,
                Content = JsonSerializer.Serialize(json, _jsonOptions),
                ContentType = "application/json"
            };
        }
        catch
        {
            // If not valid JSON, convert to JSON string
            return new FormattedOutput
            {
                Format = OutputFormat.Json,
                Content = JsonSerializer.Serialize(new { output = rawOutput }),
                ContentType = "application/json"
            };
        }
    }

    private FormattedOutput FormatAsXml(string rawOutput)
    {
        // Try to parse as XML first
        try
        {
            var xml = XDocument.Parse(rawOutput);
            return new FormattedOutput
            {
                Format = OutputFormat.Xml,
                Content = xml.ToString(),
                ContentType = "application/xml"
            };
        }
        catch
        {
            // If not valid XML, wrap in output element
            var doc = new XDocument(
                new XElement("output",
                    new XElement("content", rawOutput)
                )
            );
            
            return new FormattedOutput
            {
                Format = OutputFormat.Xml,
                Content = doc.ToString(),
                ContentType = "application/xml"
            };
        }
    }

    private FormattedOutput FormatAsYaml(string rawOutput)
    {
        // Try to parse as YAML first
        try
        {
            var data = _yamlDeserializer.Deserialize<object>(rawOutput);
            return new FormattedOutput
            {
                Format = OutputFormat.Yaml,
                Content = _yamlSerializer.Serialize(data),
                ContentType = "application/yaml"
            };
        }
        catch
        {
            // If not valid YAML, convert to YAML format
            var data = new Dictionary<string, object> { ["output"] = rawOutput };
            return new FormattedOutput
            {
                Format = OutputFormat.Yaml,
                Content = _yamlSerializer.Serialize(data),
                ContentType = "application/yaml"
            };
        }
    }

    private FormattedOutput FormatAsTable(string rawOutput)
    {
        // Tables are expected to be pre-formatted by the output::table command
        // Just ensure proper formatting
        return new FormattedOutput
        {
            Format = OutputFormat.Table,
            Content = rawOutput,
            ContentType = "text/plain",
            IsTabular = true
        };
    }

    private FormattedOutput FormatAsCsv(string rawOutput)
    {
        try
        {
            // Process output to detect structured data
            var data = ProcessOutput(rawOutput);
            
            // Convert to CSV based on data type
            string csvContent;
            if (data is Dictionary<string, object> dict)
            {
                // Single row from dictionary
                var headers = string.Join(",", dict.Keys.Select(EscapeCsvField));
                var values = string.Join(",", dict.Values.Select(v => EscapeCsvField(v?.ToString() ?? "")));
                csvContent = $"{headers}\n{values}";
            }
            else if (data is List<object> list && list.Count > 0)
            {
                // Multiple rows from list
                if (list[0] is Dictionary<string, object> firstRow)
                {
                    // List of dictionaries - extract headers from first row
                    var headers = string.Join(",", firstRow.Keys.Select(EscapeCsvField));
                    var rows = new List<string> { headers };
                    
                    foreach (var item in list)
                    {
                        if (item is Dictionary<string, object> row)
                        {
                            var values = firstRow.Keys.Select(key => 
                                row.TryGetValue(key, out var value) ? EscapeCsvField(value?.ToString() ?? "") : ""
                            );
                            rows.Add(string.Join(",", values));
                        }
                    }
                    
                    csvContent = string.Join("\n", rows);
                }
                else
                {
                    // Simple list - single column
                    csvContent = "Value\n" + string.Join("\n", list.Select(v => EscapeCsvField(v?.ToString() ?? "")));
                }
            }
            else
            {
                // Plain text - single cell
                csvContent = "Content\n" + EscapeCsvField(rawOutput);
            }
            
            return new FormattedOutput
            {
                Format = OutputFormat.Csv,
                Content = csvContent,
                ContentType = "text/csv",
                IsTabular = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to format as CSV, using plain text");
            return new FormattedOutput
            {
                Format = OutputFormat.Csv,
                Content = "Content\n" + EscapeCsvField(rawOutput),
                ContentType = "text/csv",
                IsTabular = true
            };
        }
    }

    private FormattedOutput FormatAsMarkdown(string rawOutput)
    {
        try
        {
            // Process output to detect structured data
            var data = ProcessOutput(rawOutput);
            
            // Convert to Markdown based on data type
            string markdownContent;
            if (data is Dictionary<string, object> dict)
            {
                // Format dictionary as table
                var rows = new List<string> { "| Key | Value |", "|-----|-------|" };
                foreach (var kvp in dict)
                {
                    rows.Add($"| {EscapeMarkdown(kvp.Key)} | {EscapeMarkdown(kvp.Value?.ToString() ?? "")} |");
                }
                markdownContent = string.Join("\n", rows);
            }
            else if (data is List<object> list && list.Count > 0)
            {
                if (list[0] is Dictionary<string, object> firstRow)
                {
                    // List of dictionaries - create table
                    var headers = firstRow.Keys.ToList();
                    var headerRow = "| " + string.Join(" | ", headers.Select(EscapeMarkdown)) + " |";
                    var separatorRow = "|" + string.Join("|", headers.Select(_ => "---")) + "|";
                    
                    var rows = new List<string> { headerRow, separatorRow };
                    
                    foreach (var item in list)
                    {
                        if (item is Dictionary<string, object> row)
                        {
                            var values = headers.Select(key =>
                                row.TryGetValue(key, out var value) ? EscapeMarkdown(value?.ToString() ?? "") : ""
                            );
                            rows.Add("| " + string.Join(" | ", values) + " |");
                        }
                    }
                    
                    markdownContent = string.Join("\n", rows);
                }
                else
                {
                    // Simple list - bullet points
                    markdownContent = string.Join("\n", list.Select(v => $"- {EscapeMarkdown(v?.ToString() ?? "")}"));
                }
            }
            else
            {
                // Plain text - code block for preservation
                markdownContent = $"```\n{rawOutput}\n```";
            }
            
            return new FormattedOutput
            {
                Format = OutputFormat.Markdown,
                Content = markdownContent,
                ContentType = "text/markdown"
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to format as Markdown, using code block");
            return new FormattedOutput
            {
                Format = OutputFormat.Markdown,
                Content = $"```\n{rawOutput}\n```",
                ContentType = "text/markdown"
            };
        }
    }

    private static string EscapeCsvField(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"", StringComparison.Ordinal)}\""; 
        }
        return field;
    }

    private static string EscapeMarkdown(string text)
    {
        // Escape special Markdown characters
        return text.Replace("|", "\\|", StringComparison.Ordinal)
                   .Replace("*", "\\*", StringComparison.Ordinal)
                   .Replace("_", "\\_", StringComparison.Ordinal)
                   .Replace("`", "\\`", StringComparison.Ordinal)
                   .Replace("[", "\\[", StringComparison.Ordinal)
                   .Replace("]", "\\]", StringComparison.Ordinal);
    }
    
    /// <summary>
    /// Asynchronously formats Eagle script output based on the requested format
    /// </summary>
    public Task<FormattedOutput> FormatAsync(string rawOutput, OutputFormat format)
    {
        return Task.FromResult(Format(rawOutput, format));
    }
}