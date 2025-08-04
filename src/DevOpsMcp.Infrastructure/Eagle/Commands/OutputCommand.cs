using System;
using Eagle;
using Eagle._Attributes;
using Eagle._Commands;
using Eagle._Components.Public;
using Eagle._Containers.Public;
using Eagle._Interfaces.Public;
using DevOpsMcp.Domain.Interfaces;

namespace DevOpsMcp.Infrastructure.Eagle.Commands;

/// <summary>
/// Eagle command for formatting and outputting data in various formats
/// </summary>
[ObjectId("c4e9f3d2-8a7b-4c6e-9d1f-2b3a4c5d6e7f")]
[CommandFlags(CommandFlags.Safe)]
[ObjectGroup("mcp")]
internal sealed class OutputCommand : Default
{
    private readonly IMcpOutputCommand _mcpOutput;
    
    public OutputCommand(ICommandData commandData, IMcpOutputCommand mcpOutput) 
        : base(commandData)
    {
        _mcpOutput = mcpOutput;
    }
    
    public override ReturnCode Execute(
        Interpreter interpreter,
        IClientData? clientData,
        ArgumentList arguments,
        ref Result result)
    {
        if (arguments.Count != 3)
        {
            result = "wrong # args: should be \"mcp::output data format\"";
            return ReturnCode.Error;
        }
        
        var data = arguments[1].ToString();
        var format = arguments[2].ToString();
        
        try
        {
            // Format the output using the handler
            var formattedOutput = _mcpOutput.FormatOutput(data, format);
            
            // Return the formatted output
            result = formattedOutput;
            return ReturnCode.Ok;
        }
        catch (Exception ex)
        {
            result = $"error formatting output: {ex.Message}";
            return ReturnCode.Error;
        }
    }
}