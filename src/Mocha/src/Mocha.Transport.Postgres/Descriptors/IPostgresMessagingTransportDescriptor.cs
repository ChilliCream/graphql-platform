namespace Mocha.Transport.Postgres;

/// <summary>
/// Fluent interface for configuring a PostgreSQL messaging transport, including endpoints,
/// topology resources, middleware pipelines, conventions, and handler binding strategies.
/// </summary>
public interface IPostgresMessagingTransportDescriptor
    : IMessagingTransportDescriptor
    , IMessagingDescriptor<PostgresTransportConfiguration>
{
    /// <inheritdoc cref="IMessagingTransportDescriptor.ModifyOptions(Action{TransportOptions})"/>
    new IPostgresMessagingTransportDescriptor ModifyOptions(Action<TransportOptions> configure);

    /// <inheritdoc cref="IMessagingTransportDescriptor.Schema(string)"/>
    new IPostgresMessagingTransportDescriptor Schema(string schema);

    /// <inheritdoc cref="IMessagingTransportDescriptor.BindHandlersImplicitly"/>
    new IPostgresMessagingTransportDescriptor BindHandlersImplicitly();

    /// <inheritdoc cref="IMessagingTransportDescriptor.BindHandlersExplicitly"/>
    new IPostgresMessagingTransportDescriptor BindHandlersExplicitly();

    /// <summary>
    /// Declares or retrieves a receive endpoint with the specified name.
    /// </summary>
    /// <param name="name">The endpoint name, which also defaults as the queue name.</param>
    /// <returns>A descriptor for further configuring the receive endpoint.</returns>
    IPostgresReceiveEndpointDescriptor Endpoint(string name);

    /// <summary>
    /// Declares or retrieves a dispatch endpoint with the specified name.
    /// </summary>
    /// <param name="name">The endpoint name, which also defaults as the topic name.</param>
    /// <returns>A descriptor for further configuring the dispatch endpoint.</returns>
    IPostgresDispatchEndpointDescriptor DispatchEndpoint(string name);

    /// <summary>
    /// Declares a topic in the PostgreSQL topology.
    /// </summary>
    /// <param name="name">The topic name.</param>
    /// <returns>A descriptor for further configuring the topic.</returns>
    IPostgresTopicDescriptor DeclareTopic(string name);

    /// <summary>
    /// Declares a queue in the PostgreSQL topology.
    /// </summary>
    /// <param name="name">The queue name.</param>
    /// <returns>A descriptor for further configuring the queue.</returns>
    IPostgresQueueDescriptor DeclareQueue(string name);

    /// <summary>
    /// Declares a subscription that routes messages from a topic to a queue in the PostgreSQL topology.
    /// </summary>
    /// <param name="topic">The source topic name.</param>
    /// <param name="queue">The destination queue name.</param>
    /// <returns>A descriptor for further configuring the subscription.</returns>
    IPostgresSubscriptionDescriptor DeclareSubscription(string topic, string queue);

    /// <summary>
    /// Sets the PostgreSQL connection string for this transport.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IPostgresMessagingTransportDescriptor ConnectionString(string connectionString);

    /// <summary>
    /// Configures bus-level defaults that are applied to all auto-provisioned queues and topics.
    /// </summary>
    /// <param name="configure">A delegate that configures the bus defaults.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IPostgresMessagingTransportDescriptor ConfigureDefaults(Action<PostgresBusDefaults> configure);

    /// <summary>
    /// Sets whether topology resources should be automatically provisioned in the database.
    /// When disabled, topics, queues, and subscriptions must exist before the transport starts.
    /// Individual resources can override this setting via their own <c>AutoProvision</c> method.
    /// </summary>
    /// <param name="autoProvision">
    /// <c>true</c> to enable auto-provisioning (default); <c>false</c> to disable it globally.
    /// </param>
    /// <returns>The descriptor for method chaining.</returns>
    IPostgresMessagingTransportDescriptor AutoProvision(bool autoProvision = true);

    /// <inheritdoc cref="IMessagingTransportDescriptor.Name(string)"/>
    new IPostgresMessagingTransportDescriptor Name(string name);

    /// <inheritdoc cref="IMessagingTransportDescriptor.AddConvention(IConvention)"/>
    new IPostgresMessagingTransportDescriptor AddConvention(IConvention convention);

    /// <inheritdoc cref="IMessagingTransportDescriptor.IsDefaultTransport()"/>
    new IPostgresMessagingTransportDescriptor IsDefaultTransport();

    /// <inheritdoc cref="IMessagingTransportDescriptor.UseDispatch(DispatchMiddlewareConfiguration, string?, string?)"/>
    new IPostgresMessagingTransportDescriptor UseDispatch(
        DispatchMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null);

    /// <inheritdoc cref="IMessagingTransportDescriptor.UseReceive(ReceiveMiddlewareConfiguration, string?, string?)"/>
    new IPostgresMessagingTransportDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null);

    /// <inheritdoc cref="IMessagingTransportDescriptor.Handler{THandler}"/>
    new IHandlerConfigurator<IPostgresReceiveEndpointDescriptor> Handler<THandler>()
        where THandler : class, IHandler;

    /// <inheritdoc cref="IMessagingTransportDescriptor.Consumer{TConsumer}"/>
    new IConsumerConfigurator<IPostgresReceiveEndpointDescriptor> Consumer<TConsumer>()
        where TConsumer : class, IConsumer;
}
