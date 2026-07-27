namespace Mocha.Transport.InMemory;

/// <summary>
/// Fluent descriptor for configuring an in-memory queue and its receive endpoint.
/// </summary>
public interface IInMemoryQueueDescriptor : IMessagingDescriptor<InMemoryQueueDescriptorConfiguration>
{
    /// <summary>
    /// Registers a handler type on this queue's receive endpoint.
    /// </summary>
    /// <typeparam name="THandler">The handler type.</typeparam>
    /// <returns>The descriptor for method chaining.</returns>
    IInMemoryQueueDescriptor Handler<THandler>() where THandler : class, IHandler;

    /// <summary>
    /// Registers a handler type on this queue's receive endpoint.
    /// </summary>
    /// <param name="handlerType">The handler type to bind.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IInMemoryQueueDescriptor Handler(Type handlerType);

    /// <summary>
    /// Registers a consumer type on this queue's receive endpoint.
    /// </summary>
    /// <typeparam name="TConsumer">The consumer type.</typeparam>
    /// <returns>The descriptor for method chaining.</returns>
    IInMemoryQueueDescriptor Consumer<TConsumer>() where TConsumer : class, IConsumer;

    /// <summary>
    /// Registers a consumer type on this queue's receive endpoint.
    /// </summary>
    /// <param name="consumerType">The consumer type to bind.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IInMemoryQueueDescriptor Consumer(Type consumerType);

    /// <summary>
    /// Declares that this queue receives the specified message type.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <returns>The descriptor for method chaining.</returns>
    IInMemoryQueueDescriptor Receives<TMessage>();

    /// <summary>
    /// Declares that this queue receives the specified message type.
    /// </summary>
    /// <param name="messageType">The message type.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IInMemoryQueueDescriptor Receives(Type messageType);

    /// <summary>
    /// Sets the queue bind mode to <see cref="MessagingBindMode.Implicit"/>, enabling
    /// convention binds for consumed message types on this queue.
    /// </summary>
    /// <returns>The descriptor for method chaining.</returns>
    IInMemoryQueueDescriptor BindImplicitly();

    /// <summary>
    /// Sets the queue bind mode to <see cref="MessagingBindMode.Explicit"/>, suppressing
    /// convention binds for consumed message types on this queue.
    /// </summary>
    /// <returns>The descriptor for method chaining.</returns>
    IInMemoryQueueDescriptor BindExplicitly();

    /// <summary>
    /// Sets the maximum number of messages processed concurrently on this queue's endpoint.
    /// </summary>
    /// <param name="maxConcurrency">The concurrency limit.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IInMemoryQueueDescriptor MaxConcurrency(int maxConcurrency);

    /// <summary>
    /// Sets the kind of receive endpoint for this queue.
    /// </summary>
    /// <param name="kind">The endpoint kind.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IInMemoryQueueDescriptor Kind(ReceiveEndpointKind kind);

    /// <summary>
    /// Adds receive middleware to this queue's endpoint pipeline.
    /// </summary>
    /// <param name="configuration">The middleware configuration.</param>
    /// <param name="before">Optional key to insert the middleware before.</param>
    /// <param name="after">Optional key to insert the middleware after.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IInMemoryQueueDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null);

    /// <summary>
    /// Sets the fault endpoint address for this queue's receive endpoint.
    /// </summary>
    /// <param name="address">The fault endpoint address.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IInMemoryQueueDescriptor FaultEndpoint(Uri address);

    /// <summary>
    /// Disables forwarding failed messages to a fault endpoint.
    /// </summary>
    /// <returns>The descriptor for method chaining.</returns>
    IInMemoryQueueDescriptor DisableFaultEndpoint();

    /// <summary>
    /// Sets the skipped endpoint address for this queue's receive endpoint.
    /// </summary>
    /// <param name="address">The skipped endpoint address.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IInMemoryQueueDescriptor SkippedEndpoint(Uri address);

    /// <summary>
    /// Disables forwarding skipped messages to a skipped endpoint.
    /// </summary>
    /// <returns>The descriptor for method chaining.</returns>
    IInMemoryQueueDescriptor DisableSkippedEndpoint();

    /// <summary>
    /// Binds this queue to a source topic, writing the topic and binding directly to the
    /// transport topology.
    /// </summary>
    /// <param name="source">The source URI identifying the topic.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IInMemoryQueueDescriptor BindFrom(Uri source);
}
