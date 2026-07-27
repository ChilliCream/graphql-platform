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

    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.Receives(Type)" />
    new IRabbitMQReceiveEndpointDescriptor Receives(Type messageType);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.BindImplicitly" />
    new IRabbitMQReceiveEndpointDescriptor BindImplicitly();

    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.BindExplicitly" />
    new IRabbitMQReceiveEndpointDescriptor BindExplicitly();

    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.Kind" />
    new IRabbitMQReceiveEndpointDescriptor Kind(ReceiveEndpointKind kind);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.MaxConcurrency" />
    new IRabbitMQReceiveEndpointDescriptor MaxConcurrency(int maxConcurrency);

    /// <summary>
    /// Sets the address of the fault endpoint where failed messages are forwarded.
    /// </summary>
    /// <param name="address">The fault endpoint address.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQReceiveEndpointDescriptor FaultEndpoint(Uri address);

    /// <summary>
    /// Disables forwarding failed messages to a fault endpoint.
    /// </summary>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQReceiveEndpointDescriptor DisableFaultEndpoint();

    /// <summary>
    /// Sets the address of the endpoint where skipped messages are forwarded.
    /// </summary>
    /// <param name="address">The skipped endpoint address.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQReceiveEndpointDescriptor SkippedEndpoint(Uri address);

    /// <summary>
    /// Disables forwarding skipped messages to a skipped endpoint.
    /// </summary>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQReceiveEndpointDescriptor DisableSkippedEndpoint();

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
