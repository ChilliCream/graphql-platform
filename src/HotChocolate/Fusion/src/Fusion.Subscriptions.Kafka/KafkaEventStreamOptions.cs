using System.Threading.Channels;
using Confluent.Kafka;

namespace HotChocolate.Fusion.Subscriptions.Kafka;

/// <summary>
/// Configures a Kafka event stream broker.
/// </summary>
public sealed class KafkaEventStreamOptions
{
    private Func<Channel<EventMessage>> _createMessageChannel = CreateDefaultMessageChannel;

    /// <summary>
    /// Gets or sets the Kafka bootstrap servers.
    /// </summary>
    public string? BootstrapServers { get; set; }

    /// <summary>
    /// Gets or sets the security protocol.
    /// </summary>
    public SecurityProtocol? SecurityProtocol { get; set; }

    /// <summary>
    /// Gets or sets the SASL mechanism.
    /// </summary>
    public SaslMechanism? SaslMechanism { get; set; }

    /// <summary>
    /// Gets or sets the SASL user name.
    /// </summary>
    public string? SaslUsername { get; set; }

    /// <summary>
    /// Gets or sets the SASL password.
    /// </summary>
    public string? SaslPassword { get; set; }

    /// <summary>
    /// Gets or sets the SSL certificate authority location.
    /// </summary>
    public string? SslCaLocation { get; set; }

    /// <summary>
    /// Gets or sets the SSL certificate location.
    /// </summary>
    public string? SslCertificateLocation { get; set; }

    /// <summary>
    /// Gets or sets the SSL key location.
    /// </summary>
    public string? SslKeyLocation { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether SSL certificate verification is enabled.
    /// </summary>
    public bool? EnableSslCertificateVerification { get; set; }

    /// <summary>
    /// Gets or sets the prefix used for ephemeral consumer groups.
    /// </summary>
    public string GroupIdPrefix { get; set; } = "hc-fusion-";

    /// <summary>
    /// Gets or sets the automatic offset reset behavior applied when a subscription starts without a
    /// resume cursor.
    /// </summary>
    /// <remarks>
    /// For a cursor-enabled subscription this also determines where a fresh subscribe begins.
    /// <see cref="AutoOffsetReset.Latest"/> starts at the live end so only future events are
    /// delivered, while <see cref="AutoOffsetReset.Earliest"/> starts at the earliest retained event
    /// so the subscription replays history. In both cases the emitted resume cursor reflects the
    /// chosen start position.
    /// </remarks>
    public AutoOffsetReset AutoOffsetReset { get; set; } = AutoOffsetReset.Latest;

    /// <summary>
    /// Gets or sets the maximum time a single start-offset lookup may take while a cursor-tracking
    /// subscription establishes its initial position for a partition.
    /// </summary>
    public TimeSpan SeedingQueryTimeout { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Gets or sets the overall budget for establishing initial positions for every assigned
    /// partition when a cursor-tracking subscription starts. Lookups are retried until the budget is
    /// spent. If any partition still has no position the subscription fails rather than start from an
    /// incomplete cursor. Keep this well below the consumer maximum poll interval.
    /// </summary>
    public TimeSpan SeedingDeadline { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the factory used to create the per-subscription message channel.
    /// </summary>
    /// <remarks>
    /// The default channel buffers five messages and waits when full, which keeps cursor-based
    /// resume lossless. Use <see cref="CreateBoundedMessageChannel"/> for bounded drop modes so
    /// dropped <see cref="EventMessage"/> instances dispose their pooled buffers; a drop-mode channel
    /// yields at-most-once delivery where a resume skips messages dropped under backpressure.
    /// </remarks>
    public Func<Channel<EventMessage>> CreateMessageChannel
    {
        get => _createMessageChannel;
        set => _createMessageChannel = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Gets or sets the target number of records librdkafka keeps queued locally.
    /// </summary>
    public int ConsumerQueuedMinMessages { get; set; } = 1024;

    /// <summary>
    /// Gets or sets the maximum local librdkafka prefetch queue size in kilobytes.
    /// </summary>
    public int ConsumerQueuedMaxMessagesKbytes { get; set; } = 8 * 1024;

    /// <summary>
    /// Gets or sets the delay before librdkafka fetches again after its local queue is full.
    /// </summary>
    public int ConsumerFetchQueueBackoffMs { get; set; } = 10;

    /// <summary>
    /// Gets or sets a callback that can customize the Kafka consumer options.
    /// </summary>
    /// <remarks>
    /// The callback receives the options built from the configured values and must return the
    /// options that should be used to create the Kafka consumer.
    /// </remarks>
    public Func<ConsumerConfig, ConsumerConfig>? ConfigureConsumer { get; set; }

    internal Action<IReadOnlyList<TopicPartition>>? OnPartitionsAssigned { get; set; }

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
                SingleWriter = true,
                FullMode = fullMode
            },
            static message => message.Dispose());
}
