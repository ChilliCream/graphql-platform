namespace Mocha.Transport.Kafka;

/// <summary>
/// Fluent descriptor interface for configuring a Kafka dispatch endpoint.
/// </summary>
public interface IKafkaDispatchEndpointDescriptor
    : IDispatchEndpointDescriptor<KafkaDispatchEndpointConfiguration>
{
    /// <summary>
    /// Sets the target topic for this dispatch endpoint.
    /// </summary>
    /// <param name="name">The topic name.</param>
    /// <returns>This descriptor for fluent chaining.</returns>
    IKafkaDispatchEndpointDescriptor ToTopic(string name);

    /// <inheritdoc cref="IDispatchEndpointDescriptor{TConfiguration}.Send{TMessage}"/>
    new IKafkaDispatchEndpointDescriptor Send<TMessage>();

    /// <inheritdoc cref="IDispatchEndpointDescriptor{TConfiguration}.Publish{TMessage}"/>
    new IKafkaDispatchEndpointDescriptor Publish<TMessage>();

    /// <inheritdoc cref="IDispatchEndpointDescriptor{TConfiguration}.UseDispatch"/>
    new IKafkaDispatchEndpointDescriptor UseDispatch(
        DispatchMiddlewareConfiguration configuration,
        string? before = null, string? after = null);
}
