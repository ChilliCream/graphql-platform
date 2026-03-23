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
    new IInMemoryReceiveEndpointDescriptor Consumer<TConsumer>() where TConsumer : class, IConsumer;

    /// <inheritdoc />
    new IInMemoryReceiveEndpointDescriptor Kind(ReceiveEndpointKind kind);

    /// <inheritdoc />
    new IInMemoryReceiveEndpointDescriptor FaultEndpoint(string name);

    /// <inheritdoc />
    new IInMemoryReceiveEndpointDescriptor SkippedEndpoint(string name);

    /// <inheritdoc />
    new IInMemoryReceiveEndpointDescriptor MaxConcurrency(int maxConcurrency);

    /// <summary>
    /// Sets the name of the in-memory queue this endpoint will consume from.
    /// </summary>
    /// <param name="name">The queue name.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IInMemoryReceiveEndpointDescriptor Queue(string name);

    /// <inheritdoc />
    new IInMemoryReceiveEndpointDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null);
}
