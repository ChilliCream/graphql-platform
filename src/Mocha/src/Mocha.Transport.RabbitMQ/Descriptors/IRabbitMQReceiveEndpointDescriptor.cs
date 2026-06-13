namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Fluent interface for configuring a RabbitMQ receive endpoint, including handler registration,
/// queue binding, and prefetch settings.
/// </summary>
public interface IRabbitMQReceiveEndpointDescriptor : IReceiveEndpointDescriptor<RabbitMQReceiveEndpointConfiguration>
{
    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.Handler{THandler}" />
    new IRabbitMQReceiveEndpointDescriptor Handler<THandler>() where THandler : class, IHandler;

    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.Handler(Type)" />
    new IRabbitMQReceiveEndpointDescriptor Handler(Type handlerType);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.Consumer(Type)" />
    new IRabbitMQReceiveEndpointDescriptor Consumer(Type consumerType);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.Consumer{TConsumer}" />
    new IRabbitMQReceiveEndpointDescriptor Consumer<TConsumer>() where TConsumer : class, IConsumer;

    /// <inheritdoc />
    new IRabbitMQReceiveEndpointDescriptor Receives<TMessage>();

    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.Receives{TMessage}(Action{IReceiveTypeBindDescriptor})" />
    new IRabbitMQReceiveEndpointDescriptor Receives<TMessage>(Action<IReceiveTypeBindDescriptor> configure);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.Receives(Type)" />
    new IRabbitMQReceiveEndpointDescriptor Receives(Type messageType);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.AutoBind" />
    new IRabbitMQReceiveEndpointDescriptor AutoBind(bool enabled);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.BindFrom" />
    new IRabbitMQReceiveEndpointDescriptor BindFrom(Uri source, string? routingKey = null);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.Kind" />
    new IRabbitMQReceiveEndpointDescriptor Kind(ReceiveEndpointKind kind);

    /// <summary>
    /// Sets the verbatim name of the error queue satellite for this endpoint.
    /// The name is stored exactly as provided; no convention-based transformation is applied.
    /// </summary>
    /// <param name="name">The exact queue name to use for the error satellite.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQReceiveEndpointDescriptor ErrorQueue(string name);

    /// <summary>
    /// Disables the error queue satellite for this endpoint.
    /// When disabled, failed messages are not forwarded to an error queue.
    /// </summary>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQReceiveEndpointDescriptor DisableErrorQueue();

    /// <summary>
    /// Sets the verbatim name of the skipped queue satellite for this endpoint.
    /// The name is stored exactly as provided; no convention-based transformation is applied.
    /// </summary>
    /// <param name="name">The exact queue name to use for the skipped satellite.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQReceiveEndpointDescriptor SkippedQueue(string name);

    /// <summary>
    /// Disables the skipped queue satellite for this endpoint.
    /// When disabled, unrecognized messages are not forwarded to a skipped queue.
    /// </summary>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQReceiveEndpointDescriptor DisableSkippedQueue();

    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.MaxConcurrency" />
    new IRabbitMQReceiveEndpointDescriptor MaxConcurrency(int maxConcurrency);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.FaultEndpoint" />
    new IRabbitMQReceiveEndpointDescriptor FaultEndpoint(string name);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.SkippedEndpoint" />
    new IRabbitMQReceiveEndpointDescriptor SkippedEndpoint(string name);

    /// <summary>
    /// Sets the RabbitMQ queue name that this endpoint will consume from, overriding the default
    /// derived from the endpoint name.
    /// </summary>
    /// <param name="name">The queue name to bind to.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQReceiveEndpointDescriptor Queue(string name);

    /// <summary>
    /// Sets the maximum number of unacknowledged messages the broker will deliver to this
    /// endpoint's consumer.
    /// </summary>
    /// <param name="maxPrefetch">The prefetch count limit. Defaults to 100.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQReceiveEndpointDescriptor MaxPrefetch(ushort maxPrefetch);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.UseReceive" />
    new IRabbitMQReceiveEndpointDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null);
}
