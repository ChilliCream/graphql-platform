namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Fluent interface for configuring a RabbitMQ endpoint whose identity is fixed to a named queue.
/// This is the unified front-door handle returned by <c>t.Queue(name, q => ...)</c>.
/// All consumer, binding, and auto-bind members are available here; the queue name cannot be changed
/// after creation.
/// </summary>
public interface IRabbitMQQueueEndpointDescriptor : IRabbitMQReceiveEndpointDescriptor
{
    /// <inheritdoc cref="IRabbitMQReceiveEndpointDescriptor.Handler{THandler}" />
    new IRabbitMQQueueEndpointDescriptor Handler<THandler>() where THandler : class, IHandler;

    /// <inheritdoc cref="IRabbitMQReceiveEndpointDescriptor.Handler(Type)" />
    new IRabbitMQQueueEndpointDescriptor Handler(Type handlerType);

    /// <inheritdoc cref="IRabbitMQReceiveEndpointDescriptor.Consumer(Type)" />
    new IRabbitMQQueueEndpointDescriptor Consumer(Type consumerType);

    /// <inheritdoc cref="IRabbitMQReceiveEndpointDescriptor.Consumer{TConsumer}" />
    new IRabbitMQQueueEndpointDescriptor Consumer<TConsumer>() where TConsumer : class, IConsumer;

    /// <inheritdoc cref="IRabbitMQReceiveEndpointDescriptor.Receives{TMessage}()" />
    new IRabbitMQQueueEndpointDescriptor Receives<TMessage>();

    /// <inheritdoc cref="IRabbitMQReceiveEndpointDescriptor.Receives{TMessage}(Action{IReceiveTypeBindDescriptor})" />
    new IRabbitMQQueueEndpointDescriptor Receives<TMessage>(Action<IReceiveTypeBindDescriptor> configure);

    /// <inheritdoc cref="IRabbitMQReceiveEndpointDescriptor.Receives(Type)" />
    new IRabbitMQQueueEndpointDescriptor Receives(Type messageType);

    /// <inheritdoc cref="IRabbitMQReceiveEndpointDescriptor.AutoBind(bool)" />
    new IRabbitMQQueueEndpointDescriptor AutoBind(bool enabled);

    /// <inheritdoc cref="IRabbitMQReceiveEndpointDescriptor.BindFrom(Uri, string?)" />
    new IRabbitMQQueueEndpointDescriptor BindFrom(Uri source, string? routingKey = null);

    /// <inheritdoc cref="IRabbitMQReceiveEndpointDescriptor.Kind(ReceiveEndpointKind)" />
    new IRabbitMQQueueEndpointDescriptor Kind(ReceiveEndpointKind kind);

    /// <inheritdoc cref="IRabbitMQReceiveEndpointDescriptor.ErrorQueue(string)" />
    new IRabbitMQQueueEndpointDescriptor ErrorQueue(string name);

    /// <inheritdoc cref="IRabbitMQReceiveEndpointDescriptor.DisableErrorQueue()" />
    new IRabbitMQQueueEndpointDescriptor DisableErrorQueue();

    /// <inheritdoc cref="IRabbitMQReceiveEndpointDescriptor.SkippedQueue(string)" />
    new IRabbitMQQueueEndpointDescriptor SkippedQueue(string name);

    /// <inheritdoc cref="IRabbitMQReceiveEndpointDescriptor.DisableSkippedQueue()" />
    new IRabbitMQQueueEndpointDescriptor DisableSkippedQueue();

    /// <inheritdoc cref="IRabbitMQReceiveEndpointDescriptor.MaxConcurrency(int)" />
    new IRabbitMQQueueEndpointDescriptor MaxConcurrency(int maxConcurrency);

    /// <inheritdoc cref="IRabbitMQReceiveEndpointDescriptor.FaultEndpoint(string)" />
    new IRabbitMQQueueEndpointDescriptor FaultEndpoint(string name);

    /// <inheritdoc cref="IRabbitMQReceiveEndpointDescriptor.SkippedEndpoint(string)" />
    new IRabbitMQQueueEndpointDescriptor SkippedEndpoint(string name);

    /// <inheritdoc cref="IRabbitMQReceiveEndpointDescriptor.MaxPrefetch(ushort)" />
    new IRabbitMQQueueEndpointDescriptor MaxPrefetch(ushort maxPrefetch);

    /// <inheritdoc cref="IRabbitMQReceiveEndpointDescriptor.UseReceive" />
    new IRabbitMQQueueEndpointDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null);

    /// <summary>
    /// Sets whether the backing queue survives broker restarts.
    /// </summary>
    /// <param name="durable">True to make the queue durable; otherwise false.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQQueueEndpointDescriptor Durable(bool durable = true);

    /// <summary>
    /// Configures the backing queue as a quorum queue for high availability and data safety.
    /// </summary>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQQueueEndpointDescriptor Quorum();

    /// <summary>
    /// Adds an argument to the backing queue, such as <c>x-message-ttl</c> or <c>x-dead-letter-exchange</c>.
    /// </summary>
    /// <param name="key">The argument key.</param>
    /// <param name="value">The argument value.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQQueueEndpointDescriptor WithArgument(string key, object value);

    /// <summary>
    /// Sets whether the backing queue is automatically provisioned on the broker.
    /// </summary>
    /// <param name="autoProvision">True to provision the queue; otherwise false.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQQueueEndpointDescriptor AutoProvision(bool autoProvision = true);

    /// <summary>
    /// Not supported on a unified queue endpoint. The queue name is fixed at creation.
    /// Use <c>t.Queue(name, q => ...)</c> to set the queue name.
    /// </summary>
    [Obsolete(
        "Queue identity is fixed on a unified queue endpoint. "
        + "The queue name cannot be changed after creation. "
        + "Use t.Queue(name, q => ...) to configure the endpoint with a specific queue name.",
        error: true)]
    new IRabbitMQQueueEndpointDescriptor Queue(string name);
}
