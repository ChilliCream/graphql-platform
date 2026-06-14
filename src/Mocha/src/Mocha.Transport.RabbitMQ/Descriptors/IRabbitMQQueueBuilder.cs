namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Fluent builder for configuring a RabbitMQ queue that composes a topology queue descriptor with
/// a lazily created receive endpoint. Infra-only usage (no consumer or routing method called)
/// produces a declared queue in the topology without materializing a receive endpoint.
/// </summary>
public interface IRabbitMQQueueBuilder
{
    // -- Infra group (delegates to the backing queue descriptor) --

    /// <summary>
    /// Sets whether the backing queue survives broker restarts.
    /// </summary>
    /// <param name="durable">True to make the queue durable; otherwise false.</param>
    /// <returns>The builder for method chaining.</returns>
    IRabbitMQQueueBuilder Durable(bool durable = true);

    /// <summary>
    /// Configures the backing queue as a quorum queue for high availability and data safety.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    IRabbitMQQueueBuilder Quorum();

    /// <summary>
    /// Adds an argument to the backing queue, such as <c>x-message-ttl</c> or <c>x-dead-letter-exchange</c>.
    /// </summary>
    /// <param name="key">The argument key.</param>
    /// <param name="value">The argument value.</param>
    /// <returns>The builder for method chaining.</returns>
    IRabbitMQQueueBuilder WithArgument(string key, object value);

    /// <summary>
    /// Sets whether the backing queue is automatically provisioned on the broker.
    /// </summary>
    /// <param name="autoProvision">True to provision the queue; otherwise false.</param>
    /// <returns>The builder for method chaining.</returns>
    IRabbitMQQueueBuilder AutoProvision(bool autoProvision = true);

    // -- Routing group (delegates to a lazily created receive endpoint) --

    /// <summary>
    /// Registers a handler type on this queue's receive endpoint.
    /// </summary>
    /// <typeparam name="THandler">The handler type.</typeparam>
    /// <returns>The builder for method chaining.</returns>
    IRabbitMQQueueBuilder Handler<THandler>() where THandler : class, IHandler;

    /// <summary>
    /// Registers a handler type on this queue's receive endpoint.
    /// </summary>
    /// <param name="handlerType">The handler type to bind.</param>
    /// <returns>The builder for method chaining.</returns>
    IRabbitMQQueueBuilder Handler(Type handlerType);

    /// <summary>
    /// Registers a consumer type on this queue's receive endpoint.
    /// </summary>
    /// <typeparam name="TConsumer">The consumer type.</typeparam>
    /// <returns>The builder for method chaining.</returns>
    IRabbitMQQueueBuilder Consumer<TConsumer>() where TConsumer : class, IConsumer;

    /// <summary>
    /// Registers a consumer type on this queue's receive endpoint.
    /// </summary>
    /// <param name="consumerType">The consumer type to bind.</param>
    /// <returns>The builder for method chaining.</returns>
    IRabbitMQQueueBuilder Consumer(Type consumerType);

    /// <summary>
    /// Declares that this queue receives the specified message type.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <returns>The builder for method chaining.</returns>
    IRabbitMQQueueBuilder Receives<TMessage>();

    /// <summary>
    /// Declares that this queue receives the specified message type with additional binding configuration.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="configure">A delegate to configure per-type binding.</param>
    /// <returns>The builder for method chaining.</returns>
    IRabbitMQQueueBuilder Receives<TMessage>(Action<IReceiveTypeBindDescriptor> configure);

    /// <summary>
    /// Declares that this queue receives the specified message type.
    /// </summary>
    /// <param name="messageType">The message type.</param>
    /// <returns>The builder for method chaining.</returns>
    IRabbitMQQueueBuilder Receives(Type messageType);

    /// <summary>
    /// Sets whether auto-binding is enabled for this queue's receive endpoint.
    /// </summary>
    /// <param name="enabled">True to enable auto-binding; false to disable.</param>
    /// <returns>The builder for method chaining.</returns>
    IRabbitMQQueueBuilder AutoBind(bool enabled);

    /// <summary>
    /// Sets the maximum number of unacknowledged messages the broker will deliver to this
    /// queue's consumer.
    /// </summary>
    /// <param name="maxPrefetch">The prefetch count limit.</param>
    /// <returns>The builder for method chaining.</returns>
    IRabbitMQQueueBuilder MaxPrefetch(ushort maxPrefetch);

    /// <summary>
    /// Sets the kind of receive endpoint for this queue.
    /// </summary>
    /// <param name="kind">The endpoint kind.</param>
    /// <returns>The builder for method chaining.</returns>
    IRabbitMQQueueBuilder Kind(ReceiveEndpointKind kind);

    /// <summary>
    /// Sets the maximum number of messages processed concurrently on this queue's endpoint.
    /// </summary>
    /// <param name="maxConcurrency">The concurrency limit.</param>
    /// <returns>The builder for method chaining.</returns>
    IRabbitMQQueueBuilder MaxConcurrency(int maxConcurrency);

    /// <summary>
    /// Adds receive middleware to this queue's endpoint pipeline.
    /// </summary>
    /// <param name="configuration">The middleware configuration.</param>
    /// <param name="before">Optional key to insert the middleware before.</param>
    /// <param name="after">Optional key to insert the middleware after.</param>
    /// <returns>The builder for method chaining.</returns>
    IRabbitMQQueueBuilder UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null);

    /// <summary>
    /// Sets the fault endpoint address for this queue's receive endpoint.
    /// </summary>
    /// <param name="name">The fault endpoint address.</param>
    /// <returns>The builder for method chaining.</returns>
    IRabbitMQQueueBuilder FaultEndpoint(string name);

    /// <summary>
    /// Sets the skipped endpoint address for this queue's receive endpoint.
    /// </summary>
    /// <param name="name">The skipped endpoint address.</param>
    /// <returns>The builder for method chaining.</returns>
    IRabbitMQQueueBuilder SkippedEndpoint(string name);

    // -- Satellite sugar (calls EnsureEndpoint, writes to satellite config) --

    /// <summary>
    /// Sets the verbatim name of the error queue satellite for this queue's endpoint.
    /// </summary>
    /// <param name="name">The exact queue name to use for the error satellite.</param>
    /// <returns>The builder for method chaining.</returns>
    IRabbitMQQueueBuilder ErrorQueue(string name);

    /// <summary>
    /// Disables the error queue satellite for this queue's endpoint.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    IRabbitMQQueueBuilder DisableErrorQueue();

    /// <summary>
    /// Sets the verbatim name of the skipped queue satellite for this queue's endpoint.
    /// </summary>
    /// <param name="name">The exact queue name to use for the skipped satellite.</param>
    /// <returns>The builder for method chaining.</returns>
    IRabbitMQQueueBuilder SkippedQueue(string name);

    /// <summary>
    /// Disables the skipped queue satellite for this queue's endpoint.
    /// </summary>
    /// <returns>The builder for method chaining.</returns>
    IRabbitMQQueueBuilder DisableSkippedQueue();

    // -- BindFrom (infra group, writes directly to topology) --

    /// <summary>
    /// Binds this queue to a source exchange, writing the exchange and binding directly to the
    /// transport topology without materializing a receive endpoint.
    /// </summary>
    /// <param name="source">The source URI identifying the exchange.</param>
    /// <param name="routingKey">An optional routing key for the binding.</param>
    /// <returns>The builder for method chaining.</returns>
    IRabbitMQQueueBuilder BindFrom(Uri source, string? routingKey = null);

    // -- Escape hatches --

    /// <summary>
    /// Returns the underlying queue descriptor for direct topology configuration.
    /// </summary>
    /// <returns>The queue descriptor.</returns>
    IRabbitMQQueueDescriptor AsQueue();

    /// <summary>
    /// Returns the underlying receive endpoint descriptor, creating it if necessary.
    /// </summary>
    /// <returns>The receive endpoint descriptor.</returns>
    IRabbitMQReceiveEndpointDescriptor AsEndpoint();
}
