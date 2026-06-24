using System.Threading.Channels;
using NATS.Client.Core;

namespace HotChocolate.Fusion.Subscriptions.NATS;

/// <summary>
/// Configures a NATS event stream broker.
/// </summary>
public sealed class NatsEventStreamOptions
{
    private Func<Channel<EventMessage>> _createMessageChannel = CreateDefaultMessageChannel;

    /// <summary>
    /// Gets or sets the NATS server URL.
    /// </summary>
    /// <remarks>
    /// Either <see cref="Url"/> or <see cref="ConfigureConnection"/> must be configured.
    /// </remarks>
    public string? Url { get; set; }

    /// <summary>
    /// Gets or sets a callback that can customize the NATS connection options.
    /// </summary>
    /// <remarks>
    /// The callback receives the options built from the configured values and must return the
    /// options that should be used to create the NATS connection.
    /// </remarks>
    public Func<NatsOpts, NatsOpts>? ConfigureConnection { get; set; }

    /// <summary>
    /// Gets or sets the JetStream options for durable event consumption.
    /// </summary>
    /// <remarks>
    /// When this property is <c>null</c>, the broker uses core NATS pub/sub.
    /// </remarks>
    public NatsJetStreamOptions? JetStream { get; set; }

    /// <summary>
    /// Gets or sets the factory used to create the per-subscription message channel.
    /// </summary>
    /// <remarks>
    /// The channel is only used for core NATS subscriptions with multiple subjects. The default
    /// channel buffers five messages and waits when full. Use
    /// <see cref="CreateBoundedMessageChannel"/> for bounded drop modes so dropped
    /// <see cref="EventMessage"/> instances dispose their pooled buffers.
    /// </remarks>
    public Func<Channel<EventMessage>> CreateMessageChannel
    {
        get => _createMessageChannel;
        set => _createMessageChannel = value ?? throw new ArgumentNullException(nameof(value));
    }

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

/// <summary>
/// Configures JetStream consumption for a NATS event stream broker.
/// </summary>
public sealed class NatsJetStreamOptions
{
    /// <summary>
    /// Gets or sets the JetStream stream name.
    /// </summary>
    public required string Stream { get; set; }

    /// <summary>
    /// Gets or sets the durable consumer name.
    /// </summary>
    public required string DurableConsumer { get; set; }
}
