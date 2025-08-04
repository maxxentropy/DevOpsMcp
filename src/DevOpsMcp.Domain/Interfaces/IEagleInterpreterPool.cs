using System;
using System.Threading.Tasks;

namespace DevOpsMcp.Domain.Interfaces;

/// <summary>
/// Interface for managing a pool of Eagle interpreters
/// </summary>
public interface IEagleInterpreterPool : IDisposable
{
    /// <summary>
    /// Rents an interpreter from the pool
    /// </summary>
    Task<IPooledInterpreter> RentAsync();
    
    /// <summary>
    /// Returns an interpreter to the pool
    /// </summary>
    void ReturnToPool(IPooledInterpreter interpreter);
    
    /// <summary>
    /// Gets the current pool statistics
    /// </summary>
    PoolStatistics GetStatistics();
}

/// <summary>
/// Interface for a pooled interpreter
/// </summary>
public interface IPooledInterpreter : IDisposable
{
    /// <summary>
    /// Gets the script interpreter instance
    /// </summary>
    IScriptInterpreter Interpreter { get; }
    
    /// <summary>
    /// Gets the unique ID of this pooled instance
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Gets when this interpreter was created
    /// </summary>
    DateTimeOffset CreatedAt { get; }
    
    /// <summary>
    /// Gets when this interpreter was last used
    /// </summary>
    DateTimeOffset LastUsedAt { get; }
}

/// <summary>
/// Statistics about the interpreter pool
/// </summary>
public class PoolStatistics
{
    public int TotalInterpreters { get; set; }
    public int AvailableInterpreters { get; set; }
    public int InUseInterpreters { get; set; }
    public int TotalRentals { get; set; }
    public TimeSpan AverageRentalDuration { get; set; }
}