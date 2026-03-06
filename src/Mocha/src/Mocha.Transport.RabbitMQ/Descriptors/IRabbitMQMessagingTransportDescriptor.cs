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

    /// <inheritdoc cref="IMessagingTransportDescriptor.BindHandlersImplicitly" />
    new IRabbitMQMessagingTransportDescriptor BindHandlersImplicitly();

    /// <inheritdoc cref="IMessagingTransportDescriptor.BindHandlersExplicitly" />
    new IRabbitMQMessagingTransportDescriptor BindHandlersExplicitly();

    /// <summary>
    /// Sets a factory delegate that resolves an <see cref="IRabbitMQConnectionProvider"/> for creating RabbitMQ connections.
    /// </summary>
    /// <param name="connectionFactory">A factory that takes an <see cref="IServiceProvider"/> and returns a connection provider.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQMessagingTransportDescriptor ConnectionProvider(
        Func<IServiceProvider, IRabbitMQConnectionProvider> connectionFactory);

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
    /// </summary>
    /// <param name="name">The exchange name.</param>
    /// <returns>An exchange descriptor for further configuration.</returns>
    IRabbitMQExchangeDescriptor DeclareExchange(string name);

    /// <summary>
    /// Declares or retrieves a queue in the transport topology.
    /// </summary>
    /// <param name="name">The queue name.</param>
    /// <returns>A queue descriptor for further configuration.</returns>
    IRabbitMQQueueDescriptor DeclareQueue(string name);

    /// <summary>
    /// Declares or retrieves a binding between an exchange and a queue in the transport topology.
    /// </summary>
    /// <param name="exchange">The source exchange name.</param>
    /// <param name="queue">The destination queue name.</param>
    /// <returns>A binding descriptor for further configuration.</returns>
    IRabbitMQBindingDescriptor DeclareBinding(string exchange, string queue);

    /// <inheritdoc cref="IMessagingTransportDescriptor.Name" />
    new IRabbitMQMessagingTransportDescriptor Name(string name);

    /// <inheritdoc cref="IMessagingTransportDescriptor.AddConvention" />
    new IRabbitMQMessagingTransportDescriptor AddConvention(IConvention convention);

    /// <inheritdoc cref="IMessagingTransportDescriptor.IsDefaultTransport" />
    new IRabbitMQMessagingTransportDescriptor IsDefaultTransport();

    /// <inheritdoc cref="IMessagingTransportDescriptor.UseDispatch" />
    new IRabbitMQMessagingTransportDescriptor UseDispatch(DispatchMiddlewareConfiguration configuration);

    /// <inheritdoc cref="IMessagingTransportDescriptor.AppendDispatch" />
    new IRabbitMQMessagingTransportDescriptor AppendDispatch(
        string after,
        DispatchMiddlewareConfiguration configuration);

    /// <inheritdoc cref="IMessagingTransportDescriptor.PrependDispatch" />
    new IRabbitMQMessagingTransportDescriptor PrependDispatch(
        string before,
        DispatchMiddlewareConfiguration configuration);

    /// <inheritdoc cref="IMessagingTransportDescriptor.UseReceive" />
    new IRabbitMQMessagingTransportDescriptor UseReceive(ReceiveMiddlewareConfiguration configuration);

    /// <inheritdoc cref="IMessagingTransportDescriptor.AppendReceive" />
    new IRabbitMQMessagingTransportDescriptor AppendReceive(string after, ReceiveMiddlewareConfiguration configuration);

    /// <inheritdoc cref="IMessagingTransportDescriptor.PrependReceive" />
    new IRabbitMQMessagingTransportDescriptor PrependReceive(
        string before,
        ReceiveMiddlewareConfiguration configuration);
}
