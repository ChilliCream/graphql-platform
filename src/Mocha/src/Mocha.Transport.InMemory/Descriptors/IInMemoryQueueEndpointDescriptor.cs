namespace Mocha.Transport.InMemory;

/// <summary>
/// Fluent interface for configuring an in-memory endpoint whose identity is fixed to a named queue.
/// This is the unified front-door handle returned by <c>t.Queue(name, q => ...)</c>.
/// All consumer, binding, and auto-bind members are available here; the queue name cannot be changed
/// after creation.
/// </summary>
public interface IInMemoryQueueEndpointDescriptor : IInMemoryReceiveEndpointDescriptor
{
    /// <inheritdoc cref="IInMemoryReceiveEndpointDescriptor.Handler{THandler}" />
    new IInMemoryQueueEndpointDescriptor Handler<THandler>() where THandler : class, IHandler;

    /// <inheritdoc cref="IInMemoryReceiveEndpointDescriptor.Handler(Type)" />
    new IInMemoryQueueEndpointDescriptor Handler(Type handlerType);

    /// <inheritdoc cref="IInMemoryReceiveEndpointDescriptor.Consumer(Type)" />
    new IInMemoryQueueEndpointDescriptor Consumer(Type consumerType);

    /// <inheritdoc cref="IInMemoryReceiveEndpointDescriptor.Consumer{TConsumer}" />
    new IInMemoryQueueEndpointDescriptor Consumer<TConsumer>() where TConsumer : class, IConsumer;

    /// <inheritdoc cref="IInMemoryReceiveEndpointDescriptor.Receives{TMessage}()" />
    new IInMemoryQueueEndpointDescriptor Receives<TMessage>();

    /// <inheritdoc cref="IInMemoryReceiveEndpointDescriptor.Receives{TMessage}(Action{IReceiveTypeBindDescriptor})" />
    new IInMemoryQueueEndpointDescriptor Receives<TMessage>(Action<IReceiveTypeBindDescriptor> configure);

    /// <inheritdoc cref="IInMemoryReceiveEndpointDescriptor.Receives(Type)" />
    new IInMemoryQueueEndpointDescriptor Receives(Type messageType);

    /// <inheritdoc cref="IInMemoryReceiveEndpointDescriptor.AutoBind(bool)" />
    new IInMemoryQueueEndpointDescriptor AutoBind(bool enabled);

    /// <inheritdoc cref="IInMemoryReceiveEndpointDescriptor.BindFrom(Uri, string?)" />
    new IInMemoryQueueEndpointDescriptor BindFrom(Uri source, string? routingKey = null);

    /// <inheritdoc cref="IInMemoryReceiveEndpointDescriptor.Kind(ReceiveEndpointKind)" />
    new IInMemoryQueueEndpointDescriptor Kind(ReceiveEndpointKind kind);

    /// <inheritdoc cref="IInMemoryReceiveEndpointDescriptor.FaultEndpoint(string)" />
    new IInMemoryQueueEndpointDescriptor FaultEndpoint(string name);

    /// <inheritdoc cref="IInMemoryReceiveEndpointDescriptor.SkippedEndpoint(string)" />
    new IInMemoryQueueEndpointDescriptor SkippedEndpoint(string name);

    /// <inheritdoc cref="IInMemoryReceiveEndpointDescriptor.MaxConcurrency(int)" />
    new IInMemoryQueueEndpointDescriptor MaxConcurrency(int maxConcurrency);

    /// <inheritdoc cref="IInMemoryReceiveEndpointDescriptor.UseReceive" />
    new IInMemoryQueueEndpointDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null);

    /// <summary>
    /// Not supported on a unified queue endpoint. The queue name is fixed at creation.
    /// Use <c>t.Queue(name, q => ...)</c> to set the queue name.
    /// </summary>
    [Obsolete(
        "Queue identity is fixed on a unified queue endpoint. "
        + "The queue name cannot be changed after creation. "
        + "Use t.Queue(name, q => ...) to configure the endpoint with a specific queue name.",
        error: true)]
    new IInMemoryQueueEndpointDescriptor Queue(string name);
}
