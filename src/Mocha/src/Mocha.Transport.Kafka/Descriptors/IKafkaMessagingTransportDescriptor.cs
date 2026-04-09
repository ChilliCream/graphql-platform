using Confluent.Kafka;

namespace Mocha.Transport.Kafka;

/// <summary>
/// Fluent descriptor interface for configuring a Kafka messaging transport.
/// </summary>
public interface IKafkaMessagingTransportDescriptor
    : IMessagingTransportDescriptor
    , IMessagingDescriptor<KafkaTransportConfiguration>
{
    /// <inheritdoc cref="IMessagingTransportDescriptor.ModifyOptions"/>
    new IKafkaMessagingTransportDescriptor ModifyOptions(Action<TransportOptions> configure);

    /// <inheritdoc cref="IMessagingTransportDescriptor.Schema"/>
    new IKafkaMessagingTransportDescriptor Schema(string schema);

    /// <inheritdoc cref="IMessagingTransportDescriptor.BindHandlersImplicitly"/>
    new IKafkaMessagingTransportDescriptor BindHandlersImplicitly();

    /// <inheritdoc cref="IMessagingTransportDescriptor.BindHandlersExplicitly"/>
    new IKafkaMessagingTransportDescriptor BindHandlersExplicitly();

    /// <inheritdoc cref="IMessagingTransportDescriptor.Name"/>
    new IKafkaMessagingTransportDescriptor Name(string name);

    /// <inheritdoc cref="IMessagingTransportDescriptor.AddConvention"/>
    new IKafkaMessagingTransportDescriptor AddConvention(IConvention convention);

    /// <inheritdoc cref="IMessagingTransportDescriptor.IsDefaultTransport"/>
    new IKafkaMessagingTransportDescriptor IsDefaultTransport();

    /// <inheritdoc cref="IMessagingTransportDescriptor.UseDispatch"/>
    new IKafkaMessagingTransportDescriptor UseDispatch(
        DispatchMiddlewareConfiguration configuration,
        string? before = null, string? after = null);

    /// <inheritdoc cref="IMessagingTransportDescriptor.UseReceive"/>
    new IKafkaMessagingTransportDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null, string? after = null);

    /// <summary>
    /// Sets the Kafka bootstrap servers connection string.
    /// </summary>
    /// <param name="bootstrapServers">A comma-separated list of host:port pairs.</param>
    /// <returns>This descriptor for fluent chaining.</returns>
    IKafkaMessagingTransportDescriptor BootstrapServers(string bootstrapServers);

    /// <summary>
    /// Configures the Kafka producer settings.
    /// </summary>
    /// <param name="configure">A delegate to modify the producer configuration.</param>
    /// <returns>This descriptor for fluent chaining.</returns>
    IKafkaMessagingTransportDescriptor ConfigureProducer(Action<ProducerConfig> configure);

    /// <summary>
    /// Configures the Kafka consumer settings.
    /// </summary>
    /// <param name="configure">A delegate to modify the consumer configuration.</param>
    /// <returns>This descriptor for fluent chaining.</returns>
    IKafkaMessagingTransportDescriptor ConfigureConsumer(Action<ConsumerConfig> configure);

    /// <summary>
    /// Configures bus-level defaults for auto-provisioned topics.
    /// </summary>
    /// <param name="configure">A delegate to modify the bus defaults.</param>
    /// <returns>This descriptor for fluent chaining.</returns>
    IKafkaMessagingTransportDescriptor ConfigureDefaults(Action<KafkaBusDefaults> configure);

    /// <summary>
    /// Declares a receive endpoint with the specified name.
    /// </summary>
    /// <param name="name">The logical name of the receive endpoint.</param>
    /// <returns>A receive endpoint descriptor for further configuration.</returns>
    IKafkaReceiveEndpointDescriptor Endpoint(string name);

    /// <summary>
    /// Declares a dispatch endpoint with the specified name.
    /// </summary>
    /// <param name="name">The logical name of the dispatch endpoint.</param>
    /// <returns>A dispatch endpoint descriptor for further configuration.</returns>
    IKafkaDispatchEndpointDescriptor DispatchEndpoint(string name);

    /// <summary>
    /// Declares a topic with the specified name in the transport topology.
    /// </summary>
    /// <param name="name">The topic name.</param>
    /// <returns>A topic descriptor for further configuration.</returns>
    IKafkaTopicDescriptor DeclareTopic(string name);

    /// <summary>
    /// Sets whether topology resources should be auto-provisioned.
    /// </summary>
    /// <param name="autoProvision">Whether to auto-provision.</param>
    /// <returns>This descriptor for fluent chaining.</returns>
    IKafkaMessagingTransportDescriptor AutoProvision(bool autoProvision = true);
}
