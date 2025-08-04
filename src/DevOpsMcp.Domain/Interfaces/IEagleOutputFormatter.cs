using System.Collections.Generic;
using System.Threading.Tasks;
using DevOpsMcp.Domain.Eagle;

namespace DevOpsMcp.Domain.Interfaces;

/// <summary>
/// Interface for formatting Eagle script output into various structured formats
/// </summary>
public interface IEagleOutputFormatter
{
    /// <summary>
    /// Formats the raw output from Eagle scripts into structured formats
    /// </summary>
    Task<FormattedOutput> FormatAsync(string rawOutput, OutputFormat format);
    
    /// <summary>
    /// Formats the raw output synchronously
    /// </summary>
    FormattedOutput Format(string rawOutput, OutputFormat format);
    
    
    /// <summary>
    /// Injects output formatting commands into the script interpreter
    /// </summary>
    void InjectOutputCommands(IScriptInterpreter interpreter);
}

