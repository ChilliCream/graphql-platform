namespace Mocha.Transport.Kafka;

/// <summary>
/// Fluent descriptor interface for configuring a Kafka receive endpoint.
/// </summary>
public interface IKafkaReceiveEndpointDescriptor
    : IReceiveEndpointDescriptor<KafkaReceiveEndpointConfiguration>
{
    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.Handler{THandler}"/>
    new IKafkaReceiveEndpointDescriptor Handler<THandler>() where THandler : class, IHandler;

    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.Consumer{TConsumer}"/>
    new IKafkaReceiveEndpointDescriptor Consumer<TConsumer>() where TConsumer : class, IConsumer;

    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.Kind"/>
    new IKafkaReceiveEndpointDescriptor Kind(ReceiveEndpointKind kind);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.FaultEndpoint"/>
    new IKafkaReceiveEndpointDescriptor FaultEndpoint(string name);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.SkippedEndpoint"/>
    new IKafkaReceiveEndpointDescriptor SkippedEndpoint(string name);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.MaxConcurrency"/>
    new IKafkaReceiveEndpointDescriptor MaxConcurrency(int maxConcurrency);

    /// <summary>
    /// Sets the topic name for this receive endpoint.
    /// </summary>
    /// <param name="name">The topic name.</param>
    /// <returns>This descriptor for fluent chaining.</returns>
    IKafkaReceiveEndpointDescriptor Topic(string name);

    /// <summary>
    /// Sets the consumer group ID for this receive endpoint.
    /// </summary>
    /// <param name="groupId">The consumer group identifier.</param>
    /// <returns>This descriptor for fluent chaining.</returns>
    IKafkaReceiveEndpointDescriptor ConsumerGroup(string groupId);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{TConfiguration}.UseReceive"/>
    new IKafkaReceiveEndpointDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null, string? after = null);
}
