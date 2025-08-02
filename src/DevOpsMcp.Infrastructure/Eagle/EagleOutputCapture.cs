using System.Text;

namespace DevOpsMcp.Infrastructure.Eagle;

/// <summary>
/// Captures output from Eagle interpreter commands
/// </summary>
public class EagleOutputCapture
{
    private readonly StringBuilder _output = new();
    private readonly object _lock = new();

    /// <summary>
    /// Append output from Eagle
    /// </summary>
    public void AppendOutput(string? text)
    {
        if (text == null) return;
        
        lock (_lock)
        {
            _output.Append(text);
        }
    }

    /// <summary>
    /// Append output line from Eagle
    /// </summary>
    public void AppendLine(string? text)
    {
        if (text == null) return;
        
        lock (_lock)
        {
            _output.AppendLine(text);
        }
    }

    /// <summary>
    /// Get captured output
    /// </summary>
    public string GetOutput()
    {
        lock (_lock)
        {
            return _output.ToString();
        }
    }

    /// <summary>
    /// Clear captured output
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _output.Clear();
        }
    }
}