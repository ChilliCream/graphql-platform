namespace Mocha.Transport.InMemory;

/// <summary>
/// Fluent interface for configuring an in-memory receive endpoint, including its backing queue,
/// handlers, concurrency, and receive middleware pipeline.
/// </summary>
public interface IInMemoryReceiveEndpointDescriptor : IReceiveEndpointDescriptor<InMemoryReceiveEndpointConfiguration>
{
    /// <inheritdoc />
    new IInMemoryReceiveEndpointDescriptor Handler<THandler>() where THandler : class, IHandler;

    /// <inheritdoc />
    new IInMemoryReceiveEndpointDescriptor Handler(Type handlerType);

    /// <inheritdoc />
    new IInMemoryReceiveEndpointDescriptor Consumer(Type consumerType);

    /// <inheritdoc />
    new IInMemoryReceiveEndpointDescriptor Consumer<TConsumer>() where TConsumer : class, IConsumer;

    /// <inheritdoc />
    new IInMemoryReceiveEndpointDescriptor Receives<TMessage>();

    /// <inheritdoc />
    new IInMemoryReceiveEndpointDescriptor Receives<TMessage>(Action<IReceiveTypeBindDescriptor> configure);

    /// <inheritdoc />
    new IInMemoryReceiveEndpointDescriptor Receives(Type messageType);

    /// <inheritdoc />
    new IInMemoryReceiveEndpointDescriptor AutoBind(bool enabled);

    /// <inheritdoc />
    new IInMemoryReceiveEndpointDescriptor BindFrom(Uri source, string? routingKey = null);

    /// <inheritdoc />
    new IInMemoryReceiveEndpointDescriptor Kind(ReceiveEndpointKind kind);

    /// <inheritdoc />
    new IInMemoryReceiveEndpointDescriptor FaultEndpoint(string name);

    /// <inheritdoc />
    new IInMemoryReceiveEndpointDescriptor SkippedEndpoint(string name);

    /// <inheritdoc />
    new IInMemoryReceiveEndpointDescriptor MaxConcurrency(int maxConcurrency);

    /// <inheritdoc />
    new IInMemoryReceiveEndpointDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null);
}
