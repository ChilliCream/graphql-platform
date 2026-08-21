using System.Collections.Immutable;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace Mocha.Transport.Nats;

/// <summary>
/// Represents a durable JetStream pull consumer, the JetStream equivalent of a Mocha queue.
/// </summary>
public sealed class NatsConsumer : TopologyResource<NatsConsumerConfiguration>, INatsResource
{
    /// <summary>
    /// Gets the durable name of this consumer.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Gets the subjects this consumer receives.
    /// </summary>
    public ImmutableArray<string> FilterSubjects { get; private set; } = [];

    /// <summary>
    /// Gets the name of the stream this consumer reads from, or <see langword="null"/> until the
    /// owning stream has been resolved from the filter subjects at start-up.
    /// </summary>
    public string? StreamName { get; private set; }

    /// <summary>
    /// The ceiling applied when none is declared, matching JetStream's own default.
    /// </summary>
    public const long DefaultMaxAckPending = 1000;

    /// <summary>
    /// Where a consumer starts reading when none is declared.
    /// </summary>
    /// <remarks>
    /// Applies only when the consumer is created; one that already exists resumes from its own
    /// position. Use <c>DeliverFrom</c> to start anywhere else.
    /// </remarks>
    public const ConsumerConfigDeliverPolicy DefaultDeliverPolicy = ConsumerConfigDeliverPolicy.New;

    /// <summary>
    /// Gets the maximum number of unacknowledged messages in flight across every instance reading
    /// this consumer.
    /// </summary>
    public long MaxAckPending { get; private set; }

    /// <summary>
    /// Gets how often an in-flight message reports progress to extend its acknowledgement
    /// deadline, or <see langword="null"/> when progress is never reported.
    /// </summary>
    public TimeSpan? AckProgressInterval { get; private set; }

    /// <inheritdoc />
    public bool? AutoProvision { get; private set; }

    private ConsumerConfig _config = null!;

    /// <inheritdoc />
    protected override void OnInitialize(NatsConsumerConfiguration configuration)
    {
        Name = configuration.Name ?? throw new InvalidOperationException("Consumer name is required.");

        if (!NatsNaming.IsValidName(Name))
        {
            throw new InvalidOperationException(
                $"'{Name}' is not a valid JetStream consumer name. Durable names cannot contain "
                + "'.', '*', '>', whitespace or path separators.");
        }

        FilterSubjects = [.. SubjectMatcher.Collapse(configuration.FilterSubjects ?? [])];
        StreamName = configuration.StreamName;
        MaxAckPending = configuration.MaxAckPending ?? DefaultMaxAckPending;
        AckProgressInterval = configuration.AckProgressInterval;
        AutoProvision = configuration.AutoProvision;

        _config = new ConsumerConfig
        {
            DurableName = Name,
            Name = Name,
            AckPolicy = ConsumerConfigAckPolicy.Explicit,
            DeliverPolicy = configuration.DeliverPolicy ?? DefaultDeliverPolicy,
            MaxAckPending = MaxAckPending
        };

        if (FilterSubjects.Length > 0)
        {
            _config.FilterSubjects = [.. FilterSubjects];
        }

        if (configuration.AckWait is { } ackWait)
        {
            _config.AckWait = ackWait;
        }

        if (configuration.MaxDeliver is { } maxDeliver)
        {
            _config.MaxDeliver = maxDeliver;
        }

        if (configuration.Backoff is { Count: > 0 } backoff)
        {
            _config.Backoff = [.. backoff];
        }
    }

    /// <inheritdoc />
    protected override void OnComplete(NatsConsumerConfiguration configuration)
    {
        Address = NatsAddress.ForConsumer(Topology.Address, StreamName ?? "_", Name);
    }

    /// <summary>
    /// Folds a second declaration of this consumer into the existing one.
    /// </summary>
    /// <param name="configuration">The configuration to fold in.</param>
    /// <remarks>
    /// Filter subjects are unioned and settings already present win, so a <c>DeclareConsumer</c>
    /// naming this durable takes precedence over the endpoint that adopts it.
    /// </remarks>
    internal void Merge(NatsConsumerConfiguration configuration)
    {
        if (configuration.FilterSubjects is { Count: > 0 } incoming)
        {
            var subjects = SubjectMatcher.Collapse(FilterSubjects.Concat(incoming));

            FilterSubjects = [.. subjects];
            _config.FilterSubjects = [.. subjects];
        }

        AutoProvision ??= configuration.AutoProvision;

        if (StreamName is null && configuration.StreamName is { } streamName)
        {
            BindToStream(streamName);
        }
    }

    /// <summary>
    /// Binds this consumer to the stream that captures its filter subjects.
    /// </summary>
    /// <param name="streamName">The resolved stream name.</param>
    // Called during start-up once the owning stream has been resolved, which may be a stream this
    // service neither declares nor owns.
    public void BindToStream(string streamName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamName);

        StreamName = streamName;
        Address = NatsAddress.ForConsumer(Topology.Address, streamName, Name);
    }

    /// <inheritdoc />
    public async ValueTask ProvisionAsync(INatsJSContext context, CancellationToken cancellationToken)
    {
        if (StreamName is null)
        {
            throw new InvalidOperationException(
                $"The stream for consumer '{Name}' has not been resolved. Either declare the stream "
                + "explicitly or ensure a stream capturing its subjects exists.");
        }

        await context.CreateOrUpdateConsumerAsync(StreamName, _config, cancellationToken);
    }
}
