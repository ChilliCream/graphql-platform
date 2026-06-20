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
    new IInMemoryReceiveEndpointDescriptor Receives(Type messageType);

    /// <inheritdoc />
    new IInMemoryReceiveEndpointDescriptor BindImplicitly();

    /// <inheritdoc />
    new IInMemoryReceiveEndpointDescriptor BindExplicitly();

    /// <inheritdoc />
    new IInMemoryReceiveEndpointDescriptor Kind(ReceiveEndpointKind kind);

    /// <summary>
    /// Sets the address of the fault endpoint where failed messages are forwarded.
    /// </summary>
    /// <param name="address">The fault endpoint address.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IInMemoryReceiveEndpointDescriptor FaultEndpoint(Uri address);

    /// <summary>
    /// Disables forwarding failed messages to a fault endpoint.
    /// </summary>
    /// <returns>The descriptor for method chaining.</returns>
    IInMemoryReceiveEndpointDescriptor DisableFaultEndpoint();

    /// <summary>
    /// Sets the address of the endpoint where skipped messages are forwarded.
    /// </summary>
    /// <param name="address">The skipped endpoint address.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IInMemoryReceiveEndpointDescriptor SkippedEndpoint(Uri address);

    /// <summary>
    /// Disables forwarding skipped messages to a skipped endpoint.
    /// </summary>
    /// <returns>The descriptor for method chaining.</returns>
    IInMemoryReceiveEndpointDescriptor DisableSkippedEndpoint();

    /// <inheritdoc />
    new IInMemoryReceiveEndpointDescriptor MaxConcurrency(int maxConcurrency);

    /// <inheritdoc />
    new IInMemoryReceiveEndpointDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null);
}
