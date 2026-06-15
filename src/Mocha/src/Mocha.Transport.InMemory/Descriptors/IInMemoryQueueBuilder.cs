namespace Mocha.Transport.InMemory;

/// <summary>
/// Fluent builder for configuring an in-memory queue that composes a topology queue descriptor with
/// a lazily created receive endpoint. Infra-only usage (no consumer or routing method called)
/// produces a declared queue in the topology without materializing a receive endpoint.
/// </summary>
public interface IInMemoryQueueBuilder
{
    // -- Routing group (delegates to a lazily created receive endpoint) --

    /// <summary>
    /// Registers a handler type on this queue's receive endpoint.
    /// </summary>
    /// <typeparam name="THandler">The handler type.</typeparam>
    /// <returns>The builder for method chaining.</returns>
    IInMemoryQueueBuilder Handler<THandler>() where THandler : class, IHandler;

    /// <summary>
    /// Registers a handler type on this queue's receive endpoint.
    /// </summary>
    /// <param name="handlerType">The handler type to bind.</param>
    /// <returns>The builder for method chaining.</returns>
    IInMemoryQueueBuilder Handler(Type handlerType);

    /// <summary>
    /// Registers a consumer type on this queue's receive endpoint.
    /// </summary>
    /// <typeparam name="TConsumer">The consumer type.</typeparam>
    /// <returns>The builder for method chaining.</returns>
    IInMemoryQueueBuilder Consumer<TConsumer>() where TConsumer : class, IConsumer;

    /// <summary>
    /// Registers a consumer type on this queue's receive endpoint.
    /// </summary>
    /// <param name="consumerType">The consumer type to bind.</param>
    /// <returns>The builder for method chaining.</returns>
    IInMemoryQueueBuilder Consumer(Type consumerType);

    /// <summary>
    /// Declares that this queue receives the specified message type.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <returns>The builder for method chaining.</returns>
    IInMemoryQueueBuilder Receives<TMessage>();

    /// <summary>
    /// Declares that this queue receives the specified message type.
    /// </summary>
    /// <param name="messageType">The message type.</param>
    /// <returns>The builder for method chaining.</returns>
    IInMemoryQueueBuilder Receives(Type messageType);

    /// <summary>
    /// Sets the queue bind mode to <see cref="MessagingBindMode.Implicit"/>, enabling
    /// convention binds for consumed message types on this queue.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    IInMemoryQueueBuilder BindImplicitly();

    /// <summary>
    /// Sets the queue bind mode to <see cref="MessagingBindMode.Explicit"/>, suppressing
    /// convention binds for consumed message types on this queue.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    IInMemoryQueueBuilder BindExplicitly();

    /// <summary>
    /// Sets the maximum number of messages processed concurrently on this queue's endpoint.
    /// </summary>
    /// <param name="maxConcurrency">The concurrency limit.</param>
    /// <returns>The builder for method chaining.</returns>
    IInMemoryQueueBuilder MaxConcurrency(int maxConcurrency);

    /// <summary>
    /// Sets the kind of receive endpoint for this queue.
    /// </summary>
    /// <param name="kind">The endpoint kind.</param>
    /// <returns>The builder for method chaining.</returns>
    IInMemoryQueueBuilder Kind(ReceiveEndpointKind kind);

    /// <summary>
    /// Adds receive middleware to this queue's endpoint pipeline.
    /// </summary>
    /// <param name="configuration">The middleware configuration.</param>
    /// <param name="before">Optional key to insert the middleware before.</param>
    /// <param name="after">Optional key to insert the middleware after.</param>
    /// <returns>The builder for method chaining.</returns>
    IInMemoryQueueBuilder UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null);

    /// <summary>
    /// Sets the fault endpoint address for this queue's receive endpoint.
    /// </summary>
    /// <param name="name">The fault endpoint address.</param>
    /// <returns>The builder for method chaining.</returns>
    IInMemoryQueueBuilder FaultEndpoint(string name);

    /// <summary>
    /// Sets the skipped endpoint address for this queue's receive endpoint.
    /// </summary>
    /// <param name="name">The skipped endpoint address.</param>
    /// <returns>The builder for method chaining.</returns>
    IInMemoryQueueBuilder SkippedEndpoint(string name);

    // -- BindFrom (infra group, writes directly to topology) --

    /// <summary>
    /// Binds this queue to a source topic, writing the topic and binding directly to the
    /// transport topology without materializing a receive endpoint.
    /// </summary>
    /// <param name="source">The source URI identifying the topic.</param>
    /// <param name="routingKey">An optional routing key for the binding.</param>
    /// <returns>The builder for method chaining.</returns>
    IInMemoryQueueBuilder BindFrom(Uri source, string? routingKey = null);

    // -- Escape hatches --

    /// <summary>
    /// Returns the underlying queue descriptor for direct topology configuration.
    /// </summary>
    /// <returns>The queue descriptor.</returns>
    IInMemoryQueueDescriptor AsQueue();

    /// <summary>
    /// Returns the underlying receive endpoint descriptor, creating it if necessary.
    /// </summary>
    /// <returns>The receive endpoint descriptor.</returns>
    IInMemoryReceiveEndpointDescriptor AsEndpoint();
}
