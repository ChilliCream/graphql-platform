using Microsoft.Extensions.ObjectPool;

namespace Mocha.Mediator;

/// <summary>
/// Provides access to object pools used by the mediator infrastructure,
/// enabling reuse of context objects to reduce allocations.
/// </summary>
public interface IMediatorPools
{
    /// <summary>
    /// Gets the object pool for <see cref="MediatorContext"/> instances.
    /// </summary>
    ObjectPool<MediatorContext> MediatorContext { get; }
}
