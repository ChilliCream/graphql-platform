using System.Collections.Frozen;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.ObjectPool;

namespace Mocha.Mediator;

/// <summary>
/// Holds pre-compiled middleware pipelines for all registered message types.
/// Registered as a singleton and shared across all mediator instances.
/// </summary>
public sealed class MediatorRuntime : IMediatorRuntime
{
    private readonly FrozenDictionary<Type, MediatorDelegate> _pipelines;
    private readonly ObjectPool<MediatorContext> _contextPool;

    [ThreadStatic]
    private static MediatorContext? s_cached;

    internal MediatorRuntime(
        FrozenDictionary<Type, MediatorDelegate> pipelines,
        IMediatorPools pools,
        IFeatureCollection features)
    {
        _pipelines = pipelines;
        _contextPool = pools.MediatorContext;
        Features = features;
    }

    /// <summary>
    /// Gets the read-only feature collection for this mediator runtime.
    /// </summary>
    public IFeatureCollection Features { get; }

    /// <summary>
    /// Gets the compiled pipeline delegate for the specified message type.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MediatorDelegate GetPipeline(Type messageType)
    {
        if (_pipelines.TryGetValue(messageType, out var pipeline))
        {
            return pipeline;
        }

        return ThrowMissingPipeline(messageType);
    }

    /// <summary>
    /// Rents a <see cref="MediatorContext"/> from the pool.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public MediatorContext RentContext()
    {
        var context = s_cached;
        if (context is not null)
        {
            s_cached = null;
            return context;
        }

        return _contextPool.Get();
    }

    /// <summary>
    /// Returns a <see cref="MediatorContext"/> to the pool after resetting it.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReturnContext(MediatorContext context)
    {
        if (s_cached is null)
        {
            // Reset here since the thread-static path bypasses the pool policy.
            context.Reset();
            s_cached = context;
        }
        else
        {
            // The pool policy's Return calls Reset(), so no need to reset here.
            _contextPool.Return(context);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static MediatorDelegate ThrowMissingPipeline(Type messageType)
        => throw new InvalidOperationException(
            $"No pipeline registered for message type {messageType}");
}
