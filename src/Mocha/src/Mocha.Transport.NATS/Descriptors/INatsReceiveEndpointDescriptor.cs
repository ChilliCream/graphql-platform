namespace Mocha.Transport.NATS;

/// <summary>
/// Fluent interface for configuring a NATS receive endpoint, including handler registration,
/// subject binding, and prefetch settings.
/// </summary>
public interface INatsReceiveEndpointDescriptor : IReceiveEndpointDescriptor<NatsReceiveEndpointConfiguration>
{
    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.Handler{THandler}" />
    new INatsReceiveEndpointDescriptor Handler<THandler>() where THandler : class, IHandler;

    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.Consumer{TConsumer}" />
    new INatsReceiveEndpointDescriptor Consumer<TConsumer>() where TConsumer : class, IConsumer;

    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.Kind" />
    new INatsReceiveEndpointDescriptor Kind(ReceiveEndpointKind kind);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.FaultEndpoint" />
    new INatsReceiveEndpointDescriptor FaultEndpoint(string name);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.SkippedEndpoint" />
    new INatsReceiveEndpointDescriptor SkippedEndpoint(string name);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.MaxConcurrency" />
    new INatsReceiveEndpointDescriptor MaxConcurrency(int maxConcurrency);

    /// <summary>
    /// Sets the NATS subject name that this endpoint will consume from, overriding the default
    /// derived from the endpoint name.
    /// </summary>
    /// <param name="name">The subject name to bind to.</param>
    /// <returns>The descriptor for method chaining.</returns>
    INatsReceiveEndpointDescriptor Subject(string name);

    /// <summary>
    /// Sets the JetStream durable consumer name for this endpoint.
    /// </summary>
    /// <param name="name">The consumer name.</param>
    /// <returns>The descriptor for method chaining.</returns>
    INatsReceiveEndpointDescriptor ConsumerName(string name);

    /// <summary>
    /// Sets the maximum number of unacknowledged messages the consumer will receive.
    /// Maps to JetStream consumer <c>MaxAckPending</c>.
    /// </summary>
    /// <param name="maxPrefetch">The prefetch count limit. Defaults to 100.</param>
    /// <returns>The descriptor for method chaining.</returns>
    INatsReceiveEndpointDescriptor MaxPrefetch(int maxPrefetch);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.UseReceive" />
    new INatsReceiveEndpointDescriptor UseReceive(ReceiveMiddlewareConfiguration configuration);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.AppendReceive" />
    new INatsReceiveEndpointDescriptor AppendReceive(string after, ReceiveMiddlewareConfiguration configuration);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.PrependReceive" />
    new INatsReceiveEndpointDescriptor PrependReceive(string before, ReceiveMiddlewareConfiguration configuration);
}
