using System.Threading.Channels;
using Amazon.Runtime;
using Amazon.SimpleNotificationService;
using Amazon.SQS;

namespace HotChocolate.Fusion.Subscriptions.AmazonSqs;

/// <summary>
/// Configures an Amazon SQS event stream broker.
/// </summary>
public sealed class AmazonSqsEventStreamOptions
{
    private Func<Channel<EventMessage>> _createMessageChannel = CreateDefaultMessageChannel;

    /// <summary>
    /// Gets or sets the SQS endpoint override.
    /// </summary>
    /// <remarks>
    /// Use this value for LocalStack or compatible SQS endpoints. Real AWS endpoints usually only
    /// require <see cref="Region"/> and the default AWS credential chain.
    /// </remarks>
    public string? ServiceUrl { get; set; }

    /// <summary>
    /// Gets or sets the AWS region system name.
    /// </summary>
    /// <remarks>
    /// When <see cref="ServiceUrl"/> is set, this value is used as the request signing region.
    /// Otherwise it selects the AWS region endpoint.
    /// </remarks>
    public string? Region { get; set; }

    /// <summary>
    /// Gets or sets explicit AWS credentials.
    /// </summary>
    /// <remarks>
    /// LocalStack commonly uses dummy credentials such as
    /// <see cref="BasicAWSCredentials"/> with access key <c>test</c> and secret key <c>test</c>.
    /// When this property is <c>null</c> and <see cref="ServiceUrl"/> is not set, the AWS SDK default
    /// credential chain is used.
    /// </remarks>
    public AWSCredentials? Credentials { get; set; }

    /// <summary>
    /// Gets or sets a callback that creates the SQS client configuration.
    /// </summary>
    /// <remarks>
    /// The callback wins over the discrete endpoint and region properties.
    /// </remarks>
    public Func<AmazonSQSConfig>? ConfigureClient { get; set; }

    /// <summary>
    /// Gets or sets a callback that creates the SNS client configuration.
    /// </summary>
    /// <remarks>
    /// When unset, SNS fan-out uses <see cref="ServiceUrl"/> and <see cref="Region"/> like the SQS
    /// client. This callback is only used when <see cref="ResolveTopicArn"/> is configured.
    /// </remarks>
    public Func<AmazonSimpleNotificationServiceConfig>? ConfigureNotificationClient { get; set; }

    /// <summary>
    /// Gets or sets the resolver that maps each logical Fusion topic to an SNS topic ARN.
    /// </summary>
    /// <remarks>
    /// When configured, the broker subscribes each per-subscription SQS queue to the resolved SNS
    /// topic and configures the queue policy for SNS delivery. When unset, the broker runs in direct
    /// queue mode and publishers must send to the created SQS queues out of band.
    /// </remarks>
    public Func<string, string?>? ResolveTopicArn { get; set; }

    /// <summary>
    /// Gets or sets the long-poll wait time in seconds.
    /// </summary>
    public int WaitTimeSeconds { get; set; } = 20;

    /// <summary>
    /// Gets or sets the maximum number of messages received per SQS request.
    /// </summary>
    public int MaxNumberOfMessages { get; set; } = 10;

    /// <summary>
    /// Gets or sets the message visibility timeout in seconds.
    /// </summary>
    /// <remarks>
    /// Slow consumers may need a larger value to avoid duplicate delivery before messages are deleted.
    /// </remarks>
    public int VisibilityTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the prefix used for per-subscription queue names.
    /// </summary>
    /// <remarks>
    /// SQS queue names are limited to 80 characters and may only contain alphanumeric characters,
    /// hyphens, and underscores. Logical topic names are hashed into the generated queue name.
    /// Standard queues are used by this broker. FIFO queues may be added as a future per-stream option.
    /// </remarks>
    public string QueueNamePrefix { get; set; } = "fusion-sub";

    /// <summary>
    /// Gets or sets the factory used to create the per-subscription message channel.
    /// </summary>
    /// <remarks>
    /// The channel is only used for multi-topic subscriptions. Single-topic subscriptions read directly
    /// from the SQS receive loop. The default channel buffers five messages and waits when full. It
    /// uses a single reader and multiple writers because multi-topic subscriptions run one pump per
    /// queue. Use <see cref="CreateBoundedMessageChannel"/> for bounded drop modes so dropped
    /// <see cref="EventMessage"/> instances dispose their pooled buffers.
    /// </remarks>
    public Func<Channel<EventMessage>> CreateMessageChannel
    {
        get => _createMessageChannel;
        set => _createMessageChannel = value ?? throw new ArgumentNullException(nameof(value));
    }

    internal Action<string>? OnQueueReady { get; set; }

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
