namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Configuration for an Azure Event Hub messaging transport, extending the base transport configuration
/// with Event Hub-specific connection provider settings.
/// </summary>
public class EventHubTransportConfiguration : MessagingTransportConfiguration
{
    /// <summary>
    /// The default transport name used when no explicit name is specified.
    /// </summary>
    public const string DefaultName = "eventhub";

    /// <summary>
    /// The default URI schema used for Event Hub transport addresses.
    /// </summary>
    public const string DefaultSchema = "eventhub";

    /// <summary>
    /// Creates a new configuration instance with the default name and schema.
    /// </summary>
    public EventHubTransportConfiguration()
    {
        Name = DefaultName;
        Schema = DefaultSchema;
    }

    /// <summary>
    /// Gets or sets a factory delegate that resolves an <see cref="IEventHubConnectionProvider"/>
    /// from the service provider.
    /// </summary>
    /// <remarks>
    /// When <c>null</c>, the transport falls back to creating a provider from
    /// <see cref="ConnectionString"/> or <see cref="FullyQualifiedNamespace"/>.
    /// </remarks>
    public Func<IServiceProvider, IEventHubConnectionProvider>? ConnectionProvider { get; set; }

    /// <summary>
    /// Gets or sets the connection string for the Event Hub namespace.
    /// </summary>
    /// <remarks>
    /// When set, a <see cref="ConnectionStringEventHubConnectionProvider"/> is created automatically.
    /// Mutually exclusive with <see cref="FullyQualifiedNamespace"/>.
    /// </remarks>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the fully qualified namespace of the Event Hub (e.g., "mynamespace.servicebus.windows.net").
    /// </summary>
    /// <remarks>
    /// When set, a <see cref="CredentialEventHubConnectionProvider"/> is created using
    /// <c>DefaultAzureCredential</c>. Mutually exclusive with <see cref="ConnectionString"/>.
    /// </remarks>
    public string? FullyQualifiedNamespace { get; set; }

    /// <summary>
    /// Gets or sets the explicitly declared topics (Event Hub entities) for this transport.
    /// </summary>
    public List<EventHubTopicConfiguration> Topics { get; set; } = [];

    /// <summary>
    /// Gets or sets the explicitly declared subscriptions (consumer groups) for this transport.
    /// </summary>
    public List<EventHubSubscriptionConfiguration> Subscriptions { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether topology resources (hubs, consumer groups)
    /// should be automatically provisioned. When <c>null</c>, defaults to <c>true</c>.
    /// Individual resources can override this setting.
    /// </summary>
    public bool? AutoProvision { get; set; }

    /// <summary>
    /// Gets or sets the bus-level defaults applied to all auto-provisioned topics and subscriptions.
    /// </summary>
    public EventHubBusDefaults Defaults { get; set; } = new();

    /// <summary>
    /// Gets or sets a factory delegate that resolves an <see cref="ICheckpointStore"/>
    /// from the service provider. When <c>null</c>, the transport uses an in-memory checkpoint store.
    /// </summary>
    public Func<IServiceProvider, ICheckpointStore>? CheckpointStoreFactory { get; set; }

    /// <summary>
    /// Gets or sets a factory delegate that resolves an <see cref="IPartitionOwnershipStore"/>
    /// from the service provider. When <c>null</c>, the transport uses single-instance mode
    /// where all partitions are claimed by the local processor.
    /// </summary>
    public Func<IServiceProvider, IPartitionOwnershipStore>? OwnershipStoreFactory { get; set; }

    /// <summary>
    /// Gets or sets the Azure subscription ID for ARM-based auto-provisioning.
    /// Required when <see cref="AutoProvision"/> is <c>true</c>.
    /// </summary>
    public string? SubscriptionId { get; set; }

    /// <summary>
    /// Gets or sets the Azure resource group name containing the Event Hubs namespace.
    /// Required when <see cref="AutoProvision"/> is <c>true</c>.
    /// </summary>
    public string? ResourceGroupName { get; set; }

    /// <summary>
    /// Gets or sets the Event Hubs namespace name for ARM-based auto-provisioning.
    /// Required when <see cref="AutoProvision"/> is <c>true</c>.
    /// </summary>
    public string? NamespaceName { get; set; }

    /// <summary>
    /// Gets or sets the hub name used for request/reply patterns.
    /// Event Hubs do not support dynamic hub creation, so a shared hub is used.
    /// Defaults to <c>"replies"</c>.
    /// </summary>
    public string ReplyHubName { get; set; } = "replies";
}
