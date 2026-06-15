namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Fluent interface for configuring a RabbitMQ messaging transport, including connection, topology, endpoints, and middleware.
/// </summary>
public interface IRabbitMQMessagingTransportDescriptor
    : IMessagingTransportDescriptor
    , IMessagingDescriptor<RabbitMQTransportConfiguration>
{
    /// <inheritdoc cref="IMessagingTransportDescriptor.ModifyOptions" />
    new IRabbitMQMessagingTransportDescriptor ModifyOptions(Action<TransportOptions> configure);

    /// <inheritdoc cref="IMessagingTransportDescriptor.Schema" />
    new IRabbitMQMessagingTransportDescriptor Schema(string schema);

    /// <inheritdoc cref="IMessagingTransportDescriptor.BindImplicitly" />
    new IRabbitMQMessagingTransportDescriptor BindImplicitly();

    /// <inheritdoc cref="IMessagingTransportDescriptor.BindExplicitly" />
    new IRabbitMQMessagingTransportDescriptor BindExplicitly();

    /// <summary>
    /// Sets a factory delegate that resolves an <see cref="IRabbitMQConnectionProvider"/> for creating RabbitMQ connections.
    /// </summary>
    /// <param name="connectionFactory">A factory that takes an <see cref="IServiceProvider"/> and returns a connection provider.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQMessagingTransportDescriptor ConnectionProvider(
        Func<IServiceProvider, IRabbitMQConnectionProvider> connectionFactory);

    /// <summary>
    /// Configures bus-level defaults that are applied to all auto-provisioned queues and exchanges.
    /// </summary>
    /// <param name="configure">A delegate that configures the bus defaults.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQMessagingTransportDescriptor ConfigureDefaults(Action<RabbitMQBusDefaults> configure);

    /// <summary>
    /// Gets or creates a receive endpoint descriptor with the specified name.
    /// </summary>
    /// <param name="name">The endpoint name, also used as the default queue name.</param>
    /// <returns>A receive endpoint descriptor for further configuration.</returns>
    IRabbitMQReceiveEndpointDescriptor Endpoint(string name);

    /// <summary>
    /// Gets or creates a dispatch endpoint descriptor with the specified name.
    /// </summary>
    /// <param name="name">The endpoint name, also used as the default exchange name.</param>
    /// <returns>A dispatch endpoint descriptor for further configuration.</returns>
    IRabbitMQDispatchEndpointDescriptor DispatchEndpoint(string name);

    /// <summary>
    /// Declares or retrieves an exchange in the transport topology.
    /// When called multiple times with the same name, the configurations merge using these rules:
    /// declared non-null scalar properties win; convention-generated values fill the rest; arguments merge per key;
    /// AutoProvision strengthens (true wins); provenance upgrades convention to endpoint to declared.
    /// A shape conflict (both declared values differ for the same scalar property) throws <see cref="RabbitMQTopologyShapeConflictException"/>.
    /// </summary>
    /// <param name="name">The exchange name.</param>
    /// <returns>An exchange descriptor for further configuration.</returns>
    IRabbitMQExchangeDescriptor DeclareExchange(string name);

    /// <summary>
    /// Declares or retrieves a queue in the transport topology.
    /// When called multiple times with the same name, the configurations merge using these rules:
    /// declared non-null scalar properties win; convention-generated values fill the rest; arguments merge per key;
    /// AutoProvision strengthens (true wins); provenance upgrades convention to endpoint to declared.
    /// A shape conflict (both declared values differ for the same scalar property) throws <see cref="RabbitMQTopologyShapeConflictException"/>.
    /// </summary>
    /// <param name="name">The queue name.</param>
    /// <returns>A queue descriptor for further configuration.</returns>
    IRabbitMQQueueDescriptor DeclareQueue(string name);

    /// <summary>
    /// Declares a new binding between an exchange and a queue in the transport topology.
    /// Each call creates a distinct binding. To bind several routing keys between the same exchange
    /// and queue, call this multiple times and set a different <c>RoutingKey</c> on each binding.
    /// </summary>
    /// <param name="exchange">The source exchange name.</param>
    /// <param name="queue">The destination queue name.</param>
    /// <returns>A binding descriptor for further configuration.</returns>
    IRabbitMQBindingDescriptor DeclareBinding(string exchange, string queue);

    /// <summary>
    /// Sets whether topology resources should be automatically provisioned on the broker.
    /// When disabled, queues, exchanges, and bindings must exist before the transport starts.
    /// Individual resources can override this setting via their own <c>AutoProvision</c> method.
    /// </summary>
    /// <param name="autoProvision">
    /// <c>true</c> to enable auto-provisioning (default); <c>false</c> to disable it globally.
    /// </param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQMessagingTransportDescriptor AutoProvision(bool autoProvision = true);

    /// <inheritdoc cref="IMessagingTransportDescriptor.Name" />
    new IRabbitMQMessagingTransportDescriptor Name(string name);

    /// <inheritdoc cref="IMessagingTransportDescriptor.AddConvention" />
    new IRabbitMQMessagingTransportDescriptor AddConvention(IConvention convention);

    /// <inheritdoc cref="IMessagingTransportDescriptor.IsDefaultTransport" />
    new IRabbitMQMessagingTransportDescriptor IsDefaultTransport();

    /// <inheritdoc cref="IMessagingTransportDescriptor.UseDispatch" />
    new IRabbitMQMessagingTransportDescriptor UseDispatch(
        DispatchMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null);

    /// <inheritdoc cref="IMessagingTransportDescriptor.UseReceive" />
    new IRabbitMQMessagingTransportDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null);

    /// <summary>Claims a handler for this transport, creating a convention-named endpoint.</summary>
    IMessagingTransportHandlerDescriptor<IRabbitMQReceiveEndpointDescriptor> Handler<THandler>()
        where THandler : class, IHandler;

    /// <summary>Claims a consumer for this transport, creating a convention-named endpoint.</summary>
    IMessagingTransportConsumerDescriptor<IRabbitMQReceiveEndpointDescriptor> Consumer<TConsumer>()
        where TConsumer : class, IConsumer;

    /// <summary>
    /// Gets or creates a queue builder for the given queue name. The builder composes a topology
    /// queue descriptor with a lazily created receive endpoint. Infra-only usage (no routing
    /// method called) produces a declared queue without materializing a receive endpoint. Calling
    /// this method multiple times with the same name returns the same builder instance.
    /// </summary>
    /// <param name="name">The queue name, which also serves as the endpoint identity.</param>
    /// <returns>A queue builder for further configuration.</returns>
    IRabbitMQQueueBuilder Queue(string name);

    /// <summary>
    /// Gets or creates a queue builder for the given queue name and applies additional
    /// configuration through the supplied delegate.
    /// </summary>
    /// <param name="name">The queue name, which also serves as the endpoint identity.</param>
    /// <param name="configure">A delegate that configures the queue builder.</param>
    /// <returns>The transport descriptor for method chaining.</returns>
    IRabbitMQMessagingTransportDescriptor Queue(string name, Action<IRabbitMQQueueBuilder> configure);
}
