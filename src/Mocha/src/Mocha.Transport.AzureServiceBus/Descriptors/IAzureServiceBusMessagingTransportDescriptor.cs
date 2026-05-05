namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Fluent interface for configuring an Azure Service Bus messaging transport, including endpoints,
/// topology resources, middleware pipelines, conventions, and handler binding strategies.
/// </summary>
public interface IAzureServiceBusMessagingTransportDescriptor
    : IMessagingTransportDescriptor
    , IMessagingDescriptor<AzureServiceBusTransportConfiguration>
{
    /// <inheritdoc cref="IMessagingTransportDescriptor.ModifyOptions(Action{TransportOptions})"/>
    new IAzureServiceBusMessagingTransportDescriptor ModifyOptions(Action<TransportOptions> configure);

    /// <inheritdoc cref="IMessagingTransportDescriptor.Schema(string)"/>
    new IAzureServiceBusMessagingTransportDescriptor Schema(string schema);

    /// <inheritdoc cref="IMessagingTransportDescriptor.BindHandlersImplicitly"/>
    new IAzureServiceBusMessagingTransportDescriptor BindHandlersImplicitly();

    /// <inheritdoc cref="IMessagingTransportDescriptor.BindHandlersExplicitly"/>
    new IAzureServiceBusMessagingTransportDescriptor BindHandlersExplicitly();

    /// <summary>
    /// Declares or retrieves a receive endpoint with the specified name.
    /// </summary>
    /// <param name="name">The endpoint name, which also defaults as the queue name.</param>
    /// <returns>A descriptor for further configuring the receive endpoint.</returns>
    IAzureServiceBusReceiveEndpointDescriptor Endpoint(string name);

    /// <summary>
    /// Declares or retrieves a dispatch endpoint with the specified name.
    /// </summary>
    /// <param name="name">The endpoint name, which also defaults as the topic name.</param>
    /// <returns>A descriptor for further configuring the dispatch endpoint.</returns>
    IAzureServiceBusDispatchEndpointDescriptor DispatchEndpoint(string name);

    /// <summary>
    /// Declares a topic in the Azure Service Bus topology.
    /// </summary>
    /// <param name="name">The topic name.</param>
    /// <returns>A descriptor for further configuring the topic.</returns>
    IAzureServiceBusTopicDescriptor DeclareTopic(string name);

    /// <summary>
    /// Declares a queue in the Azure Service Bus topology.
    /// </summary>
    /// <param name="name">The queue name.</param>
    /// <returns>A descriptor for further configuring the queue.</returns>
    IAzureServiceBusQueueDescriptor DeclareQueue(string name);

    /// <summary>
    /// Declares a subscription that routes messages from a topic to a queue in the Azure Service Bus topology.
    /// </summary>
    /// <param name="topic">The source topic name.</param>
    /// <param name="queue">The destination queue name.</param>
    /// <returns>A descriptor for further configuring the subscription.</returns>
    IAzureServiceBusSubscriptionDescriptor DeclareSubscription(string topic, string queue);

    /// <summary>
    /// Sets the Azure Service Bus connection string for this transport.
    /// </summary>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IAzureServiceBusMessagingTransportDescriptor ConnectionString(string connectionString);

    /// <summary>
    /// Sets the fully qualified namespace and token credential for this transport.
    /// </summary>
    /// <param name="fullyQualifiedNamespace">The fully qualified namespace (e.g., "mynamespace.servicebus.windows.net").</param>
    /// <param name="credential">The token credential for authentication.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IAzureServiceBusMessagingTransportDescriptor Namespace(
        string fullyQualifiedNamespace,
        Azure.Core.TokenCredential credential);

    /// <summary>
    /// Configures bus-level defaults that are applied to all auto-provisioned queues, topics, and endpoints.
    /// </summary>
    /// <param name="configure">A delegate that configures the bus defaults.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IAzureServiceBusMessagingTransportDescriptor ConfigureDefaults(Action<AzureServiceBusBusDefaults> configure);

    /// <summary>
    /// Sets whether topology resources should be automatically provisioned on the broker.
    /// When disabled, topics, queues, and subscriptions must exist before the transport starts.
    /// Individual resources can override this setting via their own <c>AutoProvision</c> method.
    /// </summary>
    /// <param name="autoProvision">
    /// <c>true</c> to enable auto-provisioning (default); <c>false</c> to disable it globally.
    /// </param>
    /// <returns>The descriptor for method chaining.</returns>
    IAzureServiceBusMessagingTransportDescriptor AutoProvision(bool autoProvision = true);

    /// <inheritdoc cref="IMessagingTransportDescriptor.Name(string)"/>
    new IAzureServiceBusMessagingTransportDescriptor Name(string name);

    /// <inheritdoc cref="IMessagingTransportDescriptor.AddConvention(IConvention)"/>
    new IAzureServiceBusMessagingTransportDescriptor AddConvention(IConvention convention);

    /// <inheritdoc cref="IMessagingTransportDescriptor.IsDefaultTransport()"/>
    new IAzureServiceBusMessagingTransportDescriptor IsDefaultTransport();

    /// <inheritdoc cref="IMessagingTransportDescriptor.UseDispatch(DispatchMiddlewareConfiguration, string?, string?)"/>
    new IAzureServiceBusMessagingTransportDescriptor UseDispatch(
        DispatchMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null);

    /// <inheritdoc cref="IMessagingTransportDescriptor.UseReceive(ReceiveMiddlewareConfiguration, string?, string?)"/>
    new IAzureServiceBusMessagingTransportDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null);
}
