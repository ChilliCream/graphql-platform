namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Fluent descriptor for configuring a RabbitMQ queue that composes a topology queue descriptor with
/// a lazily created receive endpoint. Infra-only usage (no consumer or routing method called)
/// produces a declared queue in the topology without materializing a receive endpoint.
/// </summary>
public interface IRabbitMQQueueDescriptor : IMessagingDescriptor<RabbitMQQueueDescriptorConfiguration>
{
    /// <summary>
    /// Sets whether the backing queue survives broker restarts.
    /// </summary>
    /// <param name="durable">True to make the queue durable; otherwise false.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQQueueDescriptor Durable(bool durable = true);

    /// <summary>
    /// Configures the backing queue as a quorum queue for high availability and data safety.
    /// </summary>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQQueueDescriptor Quorum();

    /// <summary>
    /// Adds an argument to the backing queue, such as <c>x-message-ttl</c> or <c>x-dead-letter-exchange</c>.
    /// </summary>
    /// <param name="key">The argument key.</param>
    /// <param name="value">The argument value.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQQueueDescriptor WithArgument(string key, object value);

    /// <summary>
    /// Sets whether the backing queue is automatically provisioned on the broker.
    /// </summary>
    /// <param name="autoProvision">True to provision the queue; otherwise false.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQQueueDescriptor AutoProvision(bool autoProvision = true);

    /// <summary>
    /// Registers a handler type on this queue's receive endpoint.
    /// </summary>
    /// <typeparam name="THandler">The handler type.</typeparam>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQQueueDescriptor Handler<THandler>() where THandler : class, IHandler;

    /// <summary>
    /// Registers a handler type on this queue's receive endpoint.
    /// </summary>
    /// <param name="handlerType">The handler type to bind.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQQueueDescriptor Handler(Type handlerType);

    /// <summary>
    /// Registers a consumer type on this queue's receive endpoint.
    /// </summary>
    /// <typeparam name="TConsumer">The consumer type.</typeparam>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQQueueDescriptor Consumer<TConsumer>() where TConsumer : class, IConsumer;

    /// <summary>
    /// Registers a consumer type on this queue's receive endpoint.
    /// </summary>
    /// <param name="consumerType">The consumer type to bind.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQQueueDescriptor Consumer(Type consumerType);

    /// <summary>
    /// Declares that this queue receives the specified message type.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQQueueDescriptor Receives<TMessage>();

    /// <summary>
    /// Declares that this queue receives the specified message type.
    /// </summary>
    /// <param name="messageType">The message type.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQQueueDescriptor Receives(Type messageType);

    /// <summary>
    /// Sets the queue bind mode to <see cref="MessagingBindMode.Implicit"/>, enabling
    /// convention binds for consumed message types on this queue.
    /// </summary>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQQueueDescriptor BindImplicitly();

    /// <summary>
    /// Sets the queue bind mode to <see cref="MessagingBindMode.Explicit"/>, suppressing
    /// convention binds for consumed message types on this queue.
    /// </summary>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQQueueDescriptor BindExplicitly();

    /// <summary>
    /// Sets the maximum number of unacknowledged messages the broker will deliver to this
    /// queue's consumer.
    /// </summary>
    /// <param name="maxPrefetch">The prefetch count limit.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQQueueDescriptor MaxPrefetch(ushort maxPrefetch);

    /// <summary>
    /// Sets the kind of receive endpoint for this queue.
    /// </summary>
    /// <param name="kind">The endpoint kind.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQQueueDescriptor Kind(ReceiveEndpointKind kind);

    /// <summary>
    /// Sets the maximum number of messages processed concurrently on this queue's endpoint.
    /// </summary>
    /// <param name="maxConcurrency">The concurrency limit.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQQueueDescriptor MaxConcurrency(int maxConcurrency);

    /// <summary>
    /// Adds receive middleware to this queue's endpoint pipeline.
    /// </summary>
    /// <param name="configuration">The middleware configuration.</param>
    /// <param name="before">Optional key to insert the middleware before.</param>
    /// <param name="after">Optional key to insert the middleware after.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQQueueDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null);

    /// <summary>
    /// Sets the fault endpoint address for this queue's receive endpoint.
    /// </summary>
    /// <param name="name">The fault endpoint address.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQQueueDescriptor FaultEndpoint(string name);

    /// <summary>
    /// Sets the skipped endpoint address for this queue's receive endpoint.
    /// </summary>
    /// <param name="name">The skipped endpoint address.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQQueueDescriptor SkippedEndpoint(string name);

    /// <summary>
    /// Sets the verbatim name of the error queue for this queue's endpoint.
    /// </summary>
    /// <param name="name">The exact queue name to use for the error queue.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQQueueDescriptor ErrorQueue(string name);

    /// <summary>
    /// Disables the error queue for this queue's endpoint.
    /// </summary>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQQueueDescriptor DisableErrorQueue();

    /// <summary>
    /// Sets the verbatim name of the skipped queue for this queue's endpoint.
    /// </summary>
    /// <param name="name">The exact queue name to use for the skipped queue.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQQueueDescriptor SkippedQueue(string name);

    /// <summary>
    /// Disables the skipped queue for this queue's endpoint.
    /// </summary>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQQueueDescriptor DisableSkippedQueue();

    /// <summary>
    /// Binds this queue to a source exchange, writing the exchange and binding directly to the
    /// transport topology without materializing a receive endpoint.
    /// </summary>
    /// <param name="source">The source URI identifying the exchange.</param>
    /// <param name="routingKey">An optional routing key for the binding.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQQueueDescriptor BindFrom(Uri source, string? routingKey = null);
}
