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
    /// Event Hubs allows a limited number of non-exclusive readers per partition and consumer group.
    /// Use separate consumer groups for independent capacity when the service quota requires it.
    /// </remarks>
    public string ConsumerGroup { get; set; } = EventHubConsumerClient.DefaultConsumerGroupName;

    /// <summary>
    /// Gets or sets a value indicating whether subscriptions without a cursor start at the earliest
    /// retained event.
    /// </summary>
    public bool StartFromEarliest { get; set; }

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
    /// The channel is only used for multi-hub subscriptions. Single-hub subscriptions read directly
    /// from the Event Hubs SDK stream. The default channel buffers five messages and waits when
    /// full. It uses a single reader and multiple writers because multi-hub subscriptions run one
    /// pump per Event Hub.
    /// Use <see cref="CreateBoundedMessageChannel"/> for bounded drop modes so dropped
    /// <see cref="EventMessage"/> instances dispose their pooled buffers.
    /// </remarks>
    public Func<Channel<EventMessage>> CreateMessageChannel
    {
        get => _createMessageChannel;
        set => _createMessageChannel = value ?? throw new ArgumentNullException(nameof(value));
    }

    internal Action? OnReceiverReady { get; set; }

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
