namespace Mocha.Transport.InMemory;

/// <summary>
/// Fluent interface for configuring an in-memory dispatch endpoint, including its target
/// destination and dispatch middleware pipeline.
/// </summary>
public interface IInMemoryDispatchEndpointDescriptor
    : IDispatchEndpointDescriptor<InMemoryDispatchEndpointConfiguration>
{
    /// <summary>
    /// Directs this endpoint to dispatch messages to the specified in-memory queue.
    /// </summary>
    /// <param name="name">The name of the target queue.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IInMemoryDispatchEndpointDescriptor ToQueue(string name);

    /// <summary>
    /// Directs this endpoint to dispatch messages to the specified in-memory topic.
    /// </summary>
    /// <param name="name">The name of the target topic.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IInMemoryDispatchEndpointDescriptor ToTopic(string name);

    /// <inheritdoc />
    new IInMemoryDispatchEndpointDescriptor Send<TMessage>();

    /// <inheritdoc />
    new IInMemoryDispatchEndpointDescriptor Publish<TMessage>();

    /// <inheritdoc />
    new IInMemoryDispatchEndpointDescriptor UseDispatch(
        DispatchMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null);
}
