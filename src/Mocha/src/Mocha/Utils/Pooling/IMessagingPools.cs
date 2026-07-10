using Microsoft.Extensions.ObjectPool;
using Mocha.Middlewares;

namespace Mocha;

/// <summary>
/// Provides access to object pools for dispatch and receive contexts, enabling reuse of context objects to reduce allocations.
/// </summary>
public interface IMessagingPools
{
    /// <summary>
    /// Gets the object pool for <see cref="Middlewares.DispatchContext"/> instances.
    /// </summary>
    ObjectPool<DispatchContext> DispatchContext { get; }

    /// <summary>
    /// Gets the object pool for <see cref="Middlewares.ReceiveContext"/> instances.
    /// </summary>
    ObjectPool<ReceiveContext> ReceiveContext { get; }
}
