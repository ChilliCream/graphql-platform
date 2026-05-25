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

    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.Kind" />
    new IRabbitMQReceiveEndpointDescriptor Kind(ReceiveEndpointKind kind);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.FaultEndpoint" />
    new IRabbitMQReceiveEndpointDescriptor FaultEndpoint(string name);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.SkippedEndpoint" />
    new IRabbitMQReceiveEndpointDescriptor SkippedEndpoint(string name);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.MaxConcurrency" />
    new IRabbitMQReceiveEndpointDescriptor MaxConcurrency(int maxConcurrency);

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
