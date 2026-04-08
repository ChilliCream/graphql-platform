using Mocha.Features;

namespace Mocha.Mediator;

/// <summary>
/// A poolable, mutable context that flows through the mediator middleware pipeline.
/// </summary>
public sealed class MediatorContext : IMediatorContext
{
    private readonly PooledFeatureCollection _features;

    public MediatorContext()
    {
        _features = new PooledFeatureCollection(this);
    }

    /// <inheritdoc />
    public IServiceProvider Services { get; set; } = null!;

    /// <inheritdoc />
    public object Message { get; set; } = null!;

    /// <inheritdoc />
    public Type MessageType { get; set; } = null!;

    /// <inheritdoc />
    public Type ResponseType { get; set; } = null!;

    /// <inheritdoc />
    public CancellationToken CancellationToken { get; set; }

    /// <inheritdoc />
    public IFeatureCollection Features => _features;

    /// <inheritdoc />
    public object? Result { get; set; }

    /// <inheritdoc />
    IMediatorRuntime IMediatorContext.Runtime => Runtime;

    /// <summary>
    /// Gets or sets the concrete mediator runtime that owns this context.
    /// </summary>
    public MediatorRuntime Runtime { get; set; } = null!;

    /// <summary>
    /// Initializes the context for a new dispatch, setting up runtime feature defaults
    /// and the common per-dispatch properties.
    /// </summary>
    internal void Initialize(
        MediatorRuntime runtime,
        IServiceProvider serviceProvider,
        object message,
        Type messageType,
        CancellationToken cancellationToken,
        Type? responseType = null)
    {
        Runtime = runtime;
        Services = serviceProvider;
        Message = message;
        MessageType = messageType;
        ResponseType = responseType ?? typeof(void);
        CancellationToken = cancellationToken;
        _features.Initialize(runtime.Features);
    }

    /// <summary>
    /// Resets all fields for return to the pool.
    /// </summary>
    internal void Reset()
    {
        Services = null!;
        Message = null!;
        MessageType = null!;
        ResponseType = null!;
        CancellationToken = CancellationToken.None;
        Result = null;
        Runtime = null!;
        _features.Reset();
    }
}
