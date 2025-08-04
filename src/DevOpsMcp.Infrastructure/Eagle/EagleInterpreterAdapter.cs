using System;
using DevOpsMcp.Domain.Interfaces;
using Eagle;
using Eagle._Components.Public;
using Eagle._Interfaces.Public;

namespace DevOpsMcp.Infrastructure.Eagle;

/// <summary>
/// Adapter that wraps an Eagle interpreter to implement the domain interface
/// </summary>
public class EagleInterpreterAdapter : IScriptInterpreter
{
    private readonly Interpreter _interpreter;
    private readonly string _id;
    
    public EagleInterpreterAdapter(Interpreter interpreter)
    {
        _interpreter = interpreter ?? throw new ArgumentNullException(nameof(interpreter));
        _id = Guid.NewGuid().ToString();
    }
    
    public string Id => _id;
    
    public void SetVariable(string name, object value)
    {
        Result? result = null;
        _interpreter.SetVariableValue(
            VariableFlags.None,
            name,
            value?.ToString() ?? string.Empty,
            null,
            ref result);
    }
    
    public string EvaluateScript(string script)
    {
        Result? result = null;
        var code = _interpreter.EvaluateScript(script, ref result);
        
        if (code == ReturnCode.Ok)
        {
            return result?.ToString() ?? string.Empty;
        }
        
        throw new System.InvalidOperationException($"Script evaluation failed: {result}");
    }
    
    public void AddCommand(string name, object command)
    {
        if (command is ICommand eagleCommand)
        {
            Result? result = null;
            long token = 0;
            _interpreter.AddCommand(eagleCommand, null, ref token, ref result);
        }
        else
        {
            throw new ArgumentException("Command must implement ICommand interface", nameof(command));
        }
    }
    
    /// <summary>
    /// Gets the underlying Eagle interpreter
    /// </summary>
    public Interpreter UnderlyingInterpreter => _interpreter;
}