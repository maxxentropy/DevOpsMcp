namespace DevOpsMcp.Domain.Interfaces;

/// <summary>
/// Domain abstraction for a script interpreter
/// </summary>
public interface IScriptInterpreter
{
    /// <summary>
    /// Gets the unique identifier for this interpreter instance
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Gets or sets a variable in the interpreter
    /// </summary>
    void SetVariable(string name, object value);
    
    /// <summary>
    /// Evaluates a script and returns the result
    /// </summary>
    string EvaluateScript(string script);
    
    /// <summary>
    /// Adds a command to the interpreter
    /// </summary>
    void AddCommand(string name, object command);
}