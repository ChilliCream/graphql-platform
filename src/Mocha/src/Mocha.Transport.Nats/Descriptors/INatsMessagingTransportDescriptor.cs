namespace Mocha.Transport.Nats;

/// <summary>
/// Fluent descriptor for configuring the NATS JetStream transport.
/// </summary>
public interface INatsMessagingTransportDescriptor
    : IMessagingTransportDescriptor
    , IMessagingDescriptor<NatsTransportConfiguration>
{
    /// <inheritdoc cref="IMessagingTransportDescriptor.ModifyOptions" />
    new INatsMessagingTransportDescriptor ModifyOptions(Action<TransportOptions> configure);

    /// <inheritdoc cref="IMessagingTransportDescriptor.Schema" />
    new INatsMessagingTransportDescriptor Schema(string schema);

    /// <inheritdoc cref="IMessagingTransportDescriptor.Name" />
    new INatsMessagingTransportDescriptor Name(string name);

    /// <inheritdoc cref="IMessagingTransportDescriptor.BindImplicitly" />
    new INatsMessagingTransportDescriptor BindImplicitly();

    /// <inheritdoc cref="IMessagingTransportDescriptor.BindExplicitly" />
    new INatsMessagingTransportDescriptor BindExplicitly();

    /// <inheritdoc cref="IMessagingTransportDescriptor.AddConvention" />
    new INatsMessagingTransportDescriptor AddConvention(IConvention convention);

    /// <inheritdoc cref="IMessagingTransportDescriptor.IsDefaultTransport" />
    new INatsMessagingTransportDescriptor IsDefaultTransport();

    /// <inheritdoc cref="IMessagingTransportDescriptor.UseRoutingStrategy" />
    new INatsMessagingTransportDescriptor UseRoutingStrategy(Func<IServiceProvider, RoutingStrategy> factory);

    /// <inheritdoc cref="IMessagingTransportDescriptor.UseDispatch" />
    new INatsMessagingTransportDescriptor UseDispatch(
        DispatchMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null);

    /// <inheritdoc cref="IMessagingTransportDescriptor.UseReceive" />
    new INatsMessagingTransportDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null);

    /// <summary>
    /// Supplies the connection the transport publishes and consumes over.
    /// </summary>
    /// <param name="connectionProvider">A factory resolving the provider from the service provider.</param>
    /// <returns>The descriptor for method chaining.</returns>
    INatsMessagingTransportDescriptor ConnectionProvider(
        Func<IServiceProvider, INatsConnectionProvider> connectionProvider);

    /// <summary>
    /// Names the stream that captures the subjects nothing else has claimed, defaulting to the host's
    /// service name.
    /// </summary>
    /// <param name="streamName">The name, for example <c>order-service</c>, which becomes
    /// <c>ORDER_SERVICE</c>.</param>
    /// <returns>The descriptor for method chaining.</returns>
    /// <remarks>
    /// Does not scope durable consumer names, which come from the host's service name.
    /// </remarks>
    INatsMessagingTransportDescriptor StreamName(string streamName);

    /// <summary>
    /// Enables scheduled and expiring messages on the convention stream.
    /// </summary>
    /// <param name="enable">Whether to enable scheduling.</param>
    /// <returns>The descriptor for method chaining.</returns>
    /// <remarks>
    /// Requires NATS server 2.12 for schedules and 2.11 for per-message TTL. Streams declared
    /// explicitly are not affected: they must enable the same options and capture the scheduling
    /// subject themselves.
    /// </remarks>
    INatsMessagingTransportDescriptor EnableScheduling(bool enable = true);

    /// <summary>
    /// Sends the header JetStream deduplicates on with every publish, off by default.
    /// </summary>
    /// <param name="enable">Whether to send the deduplication header.</param>
    /// <returns>The descriptor for method chaining.</returns>
    /// <remarks>
    /// A stream discards a repeated identifier within its deduplication window and acknowledges the
    /// publish as though it had been stored, so a deliberate republish of the same message is
    /// suppressed as silently as an accidental one. Message deduplication otherwise belongs to the
    /// inbox, which is transport independent and scoped per consumer rather than per subject.
    /// </remarks>
    INatsMessagingTransportDescriptor EnablePublishDeduplication(bool enable = true);

    /// <summary>
    /// Controls whether streams and consumers are provisioned during start-up.
    /// </summary>
    /// <param name="autoProvision">Whether to provision topology.</param>
    /// <returns>The descriptor for method chaining.</returns>
    INatsMessagingTransportDescriptor AutoProvision(bool autoProvision = true);

    /// <summary>
    /// Configures bus-level defaults applied to the streams and consumers created by conventions.
    /// </summary>
    /// <param name="configure">A delegate that configures the bus defaults.</param>
    /// <returns>The descriptor for method chaining.</returns>
    INatsMessagingTransportDescriptor ConfigureDefaults(Action<NatsBusDefaults> configure);

    /// <summary>
    /// Declares a receive endpoint, or returns the existing declaration with the same name.
    /// </summary>
    /// <param name="name">The endpoint name.</param>
    /// <returns>The receive endpoint descriptor.</returns>
    INatsReceiveEndpointDescriptor Endpoint(string name);

    /// <summary>
    /// Claims a handler for this transport, placing it on its conventionally named endpoint.
    /// </summary>
    /// <typeparam name="THandler">The handler type.</typeparam>
    /// <returns>A descriptor for further configuring the handler's endpoint.</returns>
    IMessagingTransportHandlerDescriptor<INatsReceiveEndpointDescriptor> Handler<THandler>()
        where THandler : class, IHandler;

    /// <summary>
    /// Claims a consumer for this transport, placing it on its conventionally named endpoint.
    /// </summary>
    /// <typeparam name="TConsumer">The consumer type.</typeparam>
    /// <returns>A descriptor for further configuring the consumer's endpoint.</returns>
    IMessagingTransportConsumerDescriptor<INatsReceiveEndpointDescriptor> Consumer<TConsumer>()
        where TConsumer : class, IConsumer;

    /// <summary>
    /// Declares a stream, or returns the existing declaration with the same name.
    /// </summary>
    /// <param name="name">The stream name, for example <c>ORDER_SERVICE</c>.</param>
    /// <returns>The stream descriptor.</returns>
    INatsStreamTopologyDescriptor DeclareStream(string name);

    /// <summary>
    /// Declares a durable consumer, or returns the existing declaration with the same name.
    /// </summary>
    /// <param name="name">The durable consumer name.</param>
    /// <returns>The consumer descriptor.</returns>
    INatsConsumerTopologyDescriptor DeclareConsumer(string name);
}
