using System;
using System.Collections.Generic;
using System.Linq;
using Eagle;
using Eagle._Attributes;
using Eagle._Commands;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;

namespace DevOpsMcp.Infrastructure.Eagle.Commands;

/// <summary>
/// Eagle command for session management using persistent store
/// </summary>
[ObjectId("d5e7c8a4-3b2f-4e9d-8c1a-7f6e5d4c3b2a")]
[CommandFlags(CommandFlags.Safe)]
[ObjectGroup("mcp")]
internal sealed class SessionCommand : Default
{
    private readonly IEagleSessionManager _sessionManager;
    
    public SessionCommand(ICommandData commandData, IEagleSessionManager sessionManager) 
        : base(commandData)
    {
        _sessionManager = sessionManager;
    }
    
    public override ReturnCode Execute(
        Interpreter interpreter,
        IClientData? clientData,
        ArgumentList arguments,
        ref Result result)
    {
        if (arguments.Count < 2)
        {
            result = "wrong # args: should be \"mcp::session action ?arg ...?\"";
            return ReturnCode.Error;
        }
        
        var action = arguments[1].ToString();
        
        try
        {
            switch (action)
            {
                case "get":
                    if (arguments.Count != 3)
                    {
                        result = "wrong # args: should be \"mcp::session get key\"";
                        return ReturnCode.Error;
                    }
                    result = _sessionManager.GetValue(arguments[2].ToString());
                    return ReturnCode.Ok;
                    
                case "set":
                    if (arguments.Count != 4)
                    {
                        result = "wrong # args: should be \"mcp::session set key value\"";
                        return ReturnCode.Error;
                    }
                    _sessionManager.SetValue(arguments[2].ToString(), arguments[3].ToString());
                    result = string.Empty;
                    return ReturnCode.Ok;
                    
                case "list":
                    if (arguments.Count != 2)
                    {
                        result = "wrong # args: should be \"mcp::session list\"";
                        return ReturnCode.Error;
                    }
                    var keys = _sessionManager.List();
                    // Return as Tcl list
                    result = keys.Count > 0 
                        ? "{" + string.Join("} {", keys.Select(k => k.Replace("}", "\\}"))) + "}"
                        : "{}";
                    return ReturnCode.Ok;
                    
                case "clear":
                    if (arguments.Count != 2)
                    {
                        result = "wrong # args: should be \"mcp::session clear\"";
                        return ReturnCode.Error;
                    }
                    _sessionManager.Clear();
                    result = string.Empty;
                    return ReturnCode.Ok;
                    
                default:
                    result = $"bad action \"{action}\": must be get, set, list, or clear";
                    return ReturnCode.Error;
            }
        }
        catch (Exception ex)
        {
            result = $"session error: {ex.Message}";
            return ReturnCode.Error;
        }
    }
}