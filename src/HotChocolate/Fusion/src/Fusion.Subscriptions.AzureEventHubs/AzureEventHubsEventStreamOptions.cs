using System.Threading.Channels;
using Azure.Core;
using Azure.Identity;
using Azure.Messaging.EventHubs.Consumer;

namespace HotChocolate.Fusion.Subscriptions.AzureEventHubs;

/// <summary>
/// Configures an Azure Event Hubs event stream broker.
/// </summary>
public sealed class AzureEventHubsEventStreamOptions
{
    private Func<Channel<EventMessage>> _createMessageChannel = CreateDefaultMessageChannel;

    /// <summary>
    /// Gets or sets the namespace-scoped or entity-scoped Event Hubs connection string.
    /// </summary>
    /// <remarks>
    /// Each subscribed topic is treated as the Event Hub name. When the connection string is
    /// entity-scoped, the entity path must match the subscribed topic.
    /// </remarks>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Gets or sets the fully qualified Event Hubs namespace.
    /// </summary>
    /// <remarks>
    /// Each subscribed topic is treated as the Event Hub name. When this property is set and
    /// <see cref="Credential"/> is <c>null</c>, the broker uses <see cref="DefaultAzureCredential"/>.
    /// </remarks>
    public string? FullyQualifiedNamespace { get; set; }

    /// <summary>
    /// Gets or sets the token credential used with <see cref="FullyQualifiedNamespace"/>.
    /// </summary>
    public TokenCredential? Credential { get; set; }

    /// <summary>
    /// Gets or sets the Event Hubs consumer group.
    /// </summary>
    /// <remarks>
    /// A cursor-tracked subscription opens one reader per (hub, partition) pair, so a subscription
    /// over H hubs with P partitions each opens H times P readers against this consumer group.
    /// Event Hubs allows at most 5 concurrent non-exclusive readers per partition per consumer
    /// group, which is about 5 concurrent tracked subscribers per hub on one group. Use separate
    /// consumer groups to scale beyond that limit.
    /// </remarks>
    public string ConsumerGroup { get; set; } = EventHubConsumerClient.DefaultConsumerGroupName;

    /// <summary>
    /// Gets or sets a value indicating whether subscriptions without a cursor start at the earliest
    /// retained event.
    /// </summary>
    /// <remarks>
    /// For a cursor-enabled subscription this also determines where a fresh subscribe begins.
    /// <c>true</c> starts at each partition's beginning sequence number so the subscription replays
    /// retained history, while <c>false</c> starts at each partition's last enqueued sequence number
    /// plus one so only future events are delivered. In both cases the emitted resume cursor
    /// reflects the chosen start position.
    /// </remarks>
    public bool StartFromEarliest { get; set; }

    /// <summary>
    /// Gets or sets the maximum time a single partition metadata lookup may take while a
    /// cursor-tracking subscription establishes its initial position.
    /// </summary>
    public TimeSpan SeedingQueryTimeout { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Gets or sets the overall budget for establishing initial positions for every partition when
    /// a cursor-tracking subscription starts. If any partition still has no position the
    /// subscription fails rather than start from an incomplete cursor.
    /// </summary>
    public TimeSpan SeedingDeadline { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the interval used to discover partitions added after a cursor-tracking
    /// subscription starts.
    /// </summary>
    public TimeSpan PartitionDiscoveryInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the maximum wait time used by Event Hubs read operations.
    /// </summary>
    /// <remarks>
    /// A finite wait time allows idle reads to yield so cancellation and disposal complete
    /// predictably.
    /// </remarks>
    public TimeSpan MaximumWaitTime { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets a callback that can customize the Event Hubs consumer client options.
    /// </summary>
    /// <remarks>
    /// The callback receives a new options instance and must return the options that should be used
    /// to create each consumer client.
    /// </remarks>
    public Func<EventHubConsumerClientOptions, EventHubConsumerClientOptions>? ConfigureClientOptions
    {
        get;
        set;
    }

    /// <summary>
    /// Gets or sets the factory used to create the per-subscription message channel.
    /// </summary>
    /// <remarks>
    /// The channel carries a subscription's merged output. Passthrough single-hub subscriptions
    /// (no cursor required and no resume cursor) read directly from the Event Hubs SDK stream,
    /// while all cursor-tracking subscriptions use the channel. The default channel buffers five
    /// messages and waits when full. It uses a single reader and multiple writers because
    /// subscriptions that merge output can run multiple pumps.
    /// Use <see cref="CreateBoundedMessageChannel"/> for bounded drop modes so dropped
    /// <see cref="EventMessage"/> instances dispose their pooled buffers.
    /// </remarks>
    public Func<Channel<EventMessage>> CreateMessageChannel
    {
        get => _createMessageChannel;
        set => _createMessageChannel = value ?? throw new ArgumentNullException(nameof(value));
    }

    internal Action? OnReceiverReady { get; set; }

    internal Action<IReadOnlyList<HubPartition>>? OnPartitionsSeeded { get; set; }

    public static Channel<EventMessage> CreateDefaultMessageChannel()
        => CreateBoundedMessageChannel(capacity: 5, BoundedChannelFullMode.Wait);

    /// <summary>
    /// Creates a bounded message channel that disposes dropped messages.
    /// </summary>
    public static Channel<EventMessage> CreateBoundedMessageChannel(
        int capacity,
        BoundedChannelFullMode fullMode)
        => Channel.CreateBounded<EventMessage>(
            new BoundedChannelOptions(capacity)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = fullMode
            },
            static message => message.Dispose());
}
