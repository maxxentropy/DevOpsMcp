using DevOpsMcp.Domain.Personas;

namespace DevOpsMcp.Domain.Interfaces;

/// <summary>
/// Interface for providing rich context injection for Eagle scripts
/// </summary>
public interface IEagleContextProvider
{
    /// <summary>
    /// Injects rich context commands into a script interpreter
    /// </summary>
    void InjectRichContext(IScriptInterpreter interpreter, DevOpsContext? devOpsContext = null, string? sessionId = null);
}