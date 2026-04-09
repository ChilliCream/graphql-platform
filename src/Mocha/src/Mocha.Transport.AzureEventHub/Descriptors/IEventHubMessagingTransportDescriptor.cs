namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Fluent interface for configuring an Azure Event Hub messaging transport, including connection
/// topology, endpoints, and middleware.
/// </summary>
public interface IEventHubMessagingTransportDescriptor
    : IMessagingTransportDescriptor
    , IMessagingDescriptor<EventHubTransportConfiguration>
{
    /// <inheritdoc cref="IMessagingTransportDescriptor.ModifyOptions" />
    new IEventHubMessagingTransportDescriptor ModifyOptions(Action<TransportOptions> configure);

    /// <inheritdoc cref="IMessagingTransportDescriptor.Schema" />
    new IEventHubMessagingTransportDescriptor Schema(string schema);

    /// <inheritdoc cref="IMessagingTransportDescriptor.BindHandlersImplicitly" />
    new IEventHubMessagingTransportDescriptor BindHandlersImplicitly();

    /// <inheritdoc cref="IMessagingTransportDescriptor.BindHandlersExplicitly" />
    new IEventHubMessagingTransportDescriptor BindHandlersExplicitly();

    /// <inheritdoc cref="IMessagingTransportDescriptor.Name" />
    new IEventHubMessagingTransportDescriptor Name(string name);

    /// <inheritdoc cref="IMessagingTransportDescriptor.AddConvention" />
    new IEventHubMessagingTransportDescriptor AddConvention(IConvention convention);

    /// <inheritdoc cref="IMessagingTransportDescriptor.IsDefaultTransport" />
    new IEventHubMessagingTransportDescriptor IsDefaultTransport();

    /// <inheritdoc cref="IMessagingTransportDescriptor.UseDispatch" />
    new IEventHubMessagingTransportDescriptor UseDispatch(
        DispatchMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null);

    /// <inheritdoc cref="IMessagingTransportDescriptor.UseReceive" />
    new IEventHubMessagingTransportDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null);

    /// <summary>
    /// Sets the connection string for the Event Hub namespace.
    /// </summary>
    /// <param name="connectionString">The Event Hub namespace connection string.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IEventHubMessagingTransportDescriptor ConnectionString(string connectionString);

    /// <summary>
    /// Sets the fully qualified namespace for Azure Identity-based authentication.
    /// </summary>
    /// <param name="fullyQualifiedNamespace">
    /// The fully qualified namespace (e.g., "mynamespace.servicebus.windows.net").
    /// </param>
    /// <returns>The descriptor for method chaining.</returns>
    IEventHubMessagingTransportDescriptor Namespace(string fullyQualifiedNamespace);

    /// <summary>
    /// Sets a factory delegate that resolves an <see cref="IEventHubConnectionProvider"/> for creating Event Hub connections.
    /// </summary>
    /// <param name="connectionProvider">A factory that takes an <see cref="IServiceProvider"/> and returns a connection provider.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IEventHubMessagingTransportDescriptor ConnectionProvider(
        Func<IServiceProvider, IEventHubConnectionProvider> connectionProvider);

    /// <summary>
    /// Sets whether topology resources should be automatically provisioned.
    /// </summary>
    /// <param name="autoProvision"><c>true</c> to enable auto-provisioning (default); <c>false</c> to disable.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IEventHubMessagingTransportDescriptor AutoProvision(bool autoProvision = true);

    /// <summary>
    /// Configures bus-level defaults that are applied to all auto-provisioned topics and subscriptions.
    /// </summary>
    /// <param name="configure">A delegate that configures the bus defaults.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IEventHubMessagingTransportDescriptor ConfigureDefaults(Action<EventHubBusDefaults> configure);

    /// <summary>
    /// Gets or creates a receive endpoint descriptor with the specified name.
    /// </summary>
    /// <param name="name">The endpoint name, also used as the default hub name.</param>
    /// <returns>A receive endpoint descriptor for further configuration.</returns>
    IEventHubReceiveEndpointDescriptor Endpoint(string name);

    /// <summary>
    /// Gets or creates a dispatch endpoint descriptor with the specified name.
    /// </summary>
    /// <param name="name">The endpoint name, also used as the default hub name.</param>
    /// <returns>A dispatch endpoint descriptor for further configuration.</returns>
    IEventHubDispatchEndpointDescriptor DispatchEndpoint(string name);

    /// <summary>
    /// Configures the Azure Resource Manager coordinates for auto-provisioning Event Hubs
    /// and consumer groups. Required when <see cref="AutoProvision"/> is enabled.
    /// </summary>
    /// <param name="subscriptionId">The Azure subscription ID.</param>
    /// <param name="resourceGroupName">The resource group containing the Event Hubs namespace.</param>
    /// <param name="namespaceName">The Event Hubs namespace name.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IEventHubMessagingTransportDescriptor ResourceGroup(
        string subscriptionId,
        string resourceGroupName,
        string namespaceName);

    /// <summary>
    /// Declares or retrieves a topic (Event Hub entity) in the transport topology.
    /// </summary>
    /// <param name="name">The Event Hub entity name.</param>
    /// <returns>A topic descriptor for further configuration.</returns>
    IEventHubTopicDescriptor DeclareTopic(string name);

    /// <summary>
    /// Declares or retrieves a subscription (consumer group) in the transport topology.
    /// </summary>
    /// <param name="topicName">The Event Hub entity name.</param>
    /// <param name="consumerGroup">The consumer group name.</param>
    /// <returns>A subscription descriptor for further configuration.</returns>
    IEventHubSubscriptionDescriptor DeclareSubscription(string topicName, string consumerGroup);

    /// <summary>
    /// Sets a factory delegate that resolves an <see cref="ICheckpointStore"/> for persisting
    /// partition checkpoints.
    /// </summary>
    /// <param name="factory">A factory that takes an <see cref="IServiceProvider"/> and returns a checkpoint store.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IEventHubMessagingTransportDescriptor CheckpointStore(Func<IServiceProvider, ICheckpointStore> factory);

    /// <summary>
    /// Configures Azure Blob Storage as the checkpoint store for persisting partition checkpoints
    /// across process restarts.
    /// </summary>
    /// <param name="connectionString">The Azure Storage account connection string.</param>
    /// <param name="containerName">The blob container name for storing checkpoint blobs.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IEventHubMessagingTransportDescriptor BlobCheckpointStore(
        string connectionString,
        string containerName);

    /// <summary>
    /// Sets a factory delegate that resolves an <see cref="IPartitionOwnershipStore"/> for
    /// coordinating partition ownership across multiple processor instances.
    /// </summary>
    /// <param name="factory">A factory that takes an <see cref="IServiceProvider"/> and returns an ownership store.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IEventHubMessagingTransportDescriptor OwnershipStore(
        Func<IServiceProvider, IPartitionOwnershipStore> factory);

    /// <summary>
    /// Configures Azure Blob Storage as the partition ownership store for distributed
    /// partition balancing across multiple processor instances.
    /// </summary>
    /// <param name="connectionString">The Azure Storage account connection string.</param>
    /// <param name="containerName">The blob container name for storing ownership blobs.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IEventHubMessagingTransportDescriptor BlobOwnershipStore(
        string connectionString,
        string containerName);
}
