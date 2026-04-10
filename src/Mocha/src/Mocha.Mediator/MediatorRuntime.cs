using System.Collections.Frozen;
using System.Collections.Immutable;
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
    private readonly FrozenDictionary<Type, ImmutableArray<MediatorDelegate>> _notificationPipelines;
    private readonly ObjectPool<MediatorContext> _contextPool;

    [ThreadStatic]
    private static MediatorContext? s_cached;

    internal MediatorRuntime(
        FrozenDictionary<Type, MediatorDelegate> pipelines,
        FrozenDictionary<Type, ImmutableArray<MediatorDelegate>> notificationPipelines,
        IMediatorPools pools,
        IFeatureCollection features,
        NotificationPublishMode notificationPublishMode)
    {
        _pipelines = pipelines;
        _notificationPipelines = notificationPipelines;
        _contextPool = pools.MediatorContext;
        Features = features;
        NotificationPublishMode = notificationPublishMode;
    }

    /// <summary>
    /// Gets the read-only feature collection for this mediator runtime.
    /// </summary>
    public IFeatureCollection Features { get; }

    /// <summary>
    /// Gets the notification publish mode for this mediator runtime.
    /// </summary>
    internal NotificationPublishMode NotificationPublishMode { get; }

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

        throw ThrowHelper.MissingPipeline(messageType);
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

    /// <summary>
    /// Gets the compiled notification pipeline delegates for the specified notification type.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ImmutableArray<MediatorDelegate> GetNotificationPipelines(Type notificationType)
    {
        if (_notificationPipelines.TryGetValue(notificationType, out var pipelines))
        {
            return pipelines;
        }

        throw ThrowHelper.MissingNotificationPipeline(notificationType);
    }
}
