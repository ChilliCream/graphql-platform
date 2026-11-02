using System.Threading.Channels;
using StackExchange.Redis;

namespace HotChocolate.Fusion.Subscriptions.Redis;

/// <summary>
/// Configures a Redis Pub/Sub event stream broker.
/// </summary>
public sealed class RedisEventStreamOptions
{
    private Func<Channel<EventMessage>> _createMessageChannel = CreateDefaultMessageChannel;

    /// <summary>
    /// Gets or sets the Redis configuration string.
    /// </summary>
    /// <remarks>
    /// This value configures the broker connection. Each subscribed topic is treated as the Redis
    /// Pub/Sub channel name. When <see cref="ConfigurationOptions"/> is set, it takes precedence.
    /// </remarks>
    public string? Configuration { get; set; }

    /// <summary>
    /// Gets or sets the Redis configuration options.
    /// </summary>
    /// <remarks>
    /// This value configures the broker connection. Each subscribed topic is treated as the Redis
    /// Pub/Sub channel name. These options take precedence over <see cref="Configuration"/>.
    /// </remarks>
    public ConfigurationOptions? ConfigurationOptions { get; set; }

    /// <summary>
    /// Gets or sets a caller-supplied Redis connection multiplexer.
    /// </summary>
    /// <remarks>
    /// When set, this multiplexer is shared by the broker and remains owned by the caller. The broker
    /// does not dispose it.
    /// </remarks>
    public IConnectionMultiplexer? ConnectionMultiplexer { get; set; }

    /// <summary>
    /// Gets or sets the factory used to create the per-subscription message channel.
    /// </summary>
    /// <remarks>
    /// The default channel buffers five messages and waits when full. It uses a single reader and
    /// multiple writers because multi-channel subscriptions run one pump per Redis channel. Use
    /// <see cref="CreateBoundedMessageChannel"/> for bounded drop modes so dropped
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
