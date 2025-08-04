using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Eagle;
using Eagle._Attributes;
using Eagle._Commands;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace DevOpsMcp.Infrastructure.Eagle.Commands;

/// <summary>
/// Eagle command for calling MCP tools from scripts
/// </summary>
[ObjectId("a3f8b2c5-7d1e-4b9a-8e2c-6f5d4c3b2a1e")]
[CommandFlags(CommandFlags.Safe)]
[ObjectGroup("mcp")]
internal sealed class CallToolCommand : Default
{
    private readonly IMcpCallToolCommand _mcpCallTool;
    
    public CallToolCommand(ICommandData commandData, IMcpCallToolCommand mcpCallTool) 
        : base(commandData)
    {
        _mcpCallTool = mcpCallTool;
    }
    
    public override ReturnCode Execute(
        Interpreter interpreter,
        IClientData? clientData,
        ArgumentList arguments,
        ref Result result)
    {
        if (arguments.Count < 2)
        {
            result = "wrong # args: should be \"mcp::call_tool toolName ?args?\"";
            return ReturnCode.Error;
        }
        
        var toolName = arguments[1].ToString();
        var toolArgs = new Dictionary<string, object>();
        
        try
        {
            // Parse arguments
            if (arguments.Count == 3)
            {
                // Single argument - should be a Tcl dictionary
                var dictStr = arguments[2].ToString();
                
                // Try to parse as Tcl dictionary using Eagle's facilities
                if (!TryParseTclDictionary(interpreter, dictStr, out toolArgs))
                {
                    result = $"invalid dictionary value \"{dictStr}\"";
                    return ReturnCode.Error;
                }
            }
            else if (arguments.Count > 2)
            {
                // Multiple arguments - treat as key-value pairs
                if ((arguments.Count - 2) % 2 != 0)
                {
                    result = "args must be key-value pairs or a single dictionary";
                    return ReturnCode.Error;
                }
                
                for (int i = 2; i < arguments.Count; i += 2)
                {
                    var key = arguments[i].ToString();
                    var value = arguments[i + 1].ToString();
                    toolArgs[key] = value;
                }
            }
            
            // Call the MCP tool
            var toolResult = _mcpCallTool.CallTool(toolName, toolArgs);
            
            // Return the result as-is (should be JSON)
            result = toolResult;
            return ReturnCode.Ok;
        }
        catch (Exception ex)
        {
            result = $"error calling tool \"{toolName}\": {ex.Message}";
            return ReturnCode.Error;
        }
    }
    
    private bool TryParseTclDictionary(Interpreter interpreter, string dictStr, out Dictionary<string, object> dict)
    {
        dict = new Dictionary<string, object>();
        
        try
        {
            // Handle empty dictionary
            if (string.IsNullOrWhiteSpace(dictStr) || dictStr == "{}" || dictStr == "")
            {
                return true; // Empty dictionary is valid
            }
            
            // In Eagle, a dictionary is just a list with an even number of elements
            // where elements alternate between keys and values
            // Use the interpreter to evaluate the string as a list
            Result? listResult = null;
            var code = interpreter.EvaluateScript($"llength {{{dictStr}}}", ref listResult);
            
            if (code != ReturnCode.Ok)
            {
                return false;
            }
            
            int listLength;
            if (!int.TryParse(listResult?.ToString(), out listLength))
            {
                return false;
            }
            
            // Check if we have an even number of elements
            if (listLength % 2 != 0)
            {
                return false; // Not a valid dictionary
            }
            
            // Parse key-value pairs
            for (int i = 0; i < listLength; i += 2)
            {
                Result? keyResult = null;
                Result? valueResult = null;
                
                code = interpreter.EvaluateScript($"lindex {{{dictStr}}} {i}", ref keyResult);
                if (code != ReturnCode.Ok) return false;
                
                code = interpreter.EvaluateScript($"lindex {{{dictStr}}} {i + 1}", ref valueResult);
                if (code != ReturnCode.Ok) return false;
                
                var key = keyResult?.ToString() ?? "";
                var value = valueResult?.ToString() ?? "";
                dict[key] = value;
            }
            
            return true;
        }
        catch
        {
            return false;
        }
    }
}