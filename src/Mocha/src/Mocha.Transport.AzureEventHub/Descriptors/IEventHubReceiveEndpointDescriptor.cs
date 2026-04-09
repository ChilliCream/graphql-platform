namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Fluent interface for configuring an Event Hub receive endpoint, including handler registration
/// hub binding, and consumer group settings.
/// </summary>
public interface IEventHubReceiveEndpointDescriptor : IReceiveEndpointDescriptor<EventHubReceiveEndpointConfiguration>
{
    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.Handler{THandler}" />
    new IEventHubReceiveEndpointDescriptor Handler<THandler>() where THandler : class, IHandler;

    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.Consumer{TConsumer}" />
    new IEventHubReceiveEndpointDescriptor Consumer<TConsumer>() where TConsumer : class, IConsumer;

    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.Kind" />
    new IEventHubReceiveEndpointDescriptor Kind(ReceiveEndpointKind kind);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.FaultEndpoint" />
    new IEventHubReceiveEndpointDescriptor FaultEndpoint(string name);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.SkippedEndpoint" />
    new IEventHubReceiveEndpointDescriptor SkippedEndpoint(string name);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.MaxConcurrency" />
    new IEventHubReceiveEndpointDescriptor MaxConcurrency(int maxConcurrency);

    /// <summary>
    /// Sets the Event Hub name that this endpoint will consume from.
    /// </summary>
    /// <param name="name">The Event Hub entity name.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IEventHubReceiveEndpointDescriptor Hub(string name);

    /// <summary>
    /// Sets the consumer group used by this endpoint.
    /// </summary>
    /// <param name="consumerGroup">The consumer group name.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IEventHubReceiveEndpointDescriptor ConsumerGroup(string consumerGroup);

    /// <summary>
    /// Sets the number of events processed between checkpoints.
    /// Defaults to 100.
    /// </summary>
    /// <param name="interval">The checkpoint interval in number of events.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IEventHubReceiveEndpointDescriptor CheckpointInterval(int interval);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.UseReceive" />
    new IEventHubReceiveEndpointDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null);
}
