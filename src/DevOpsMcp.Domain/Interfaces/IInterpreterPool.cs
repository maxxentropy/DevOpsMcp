using System;
using System.Threading;
using System.Threading.Tasks;
using DevOpsMcp.Domain.Eagle;

namespace DevOpsMcp.Domain.Interfaces;

/// <summary>
/// Interface for managing a pool of script interpreters
/// </summary>
public interface IInterpreterPool : IDisposable
{
    /// <summary>
    /// Acquires an interpreter from the pool
    /// </summary>
    Task<IPooledInterpreter> AcquireAsync(EagleSecurityPolicy securityPolicy, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Releases an interpreter back to the pool
    /// </summary>
    void Release(IPooledInterpreter interpreter, bool hadError = false);
    
    /// <summary>
    /// Gets pool statistics
    /// </summary>
    PoolStatistics GetStatistics();
}