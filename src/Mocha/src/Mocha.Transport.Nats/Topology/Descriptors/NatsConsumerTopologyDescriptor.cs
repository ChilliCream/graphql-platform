using NATS.Client.JetStream.Models;

namespace Mocha.Transport.Nats;

/// <summary>
/// Default implementation of <see cref="INatsConsumerTopologyDescriptor"/>.
/// </summary>
public sealed class NatsConsumerTopologyDescriptor
    : MessagingDescriptorBase<NatsConsumerConfiguration>
    , INatsConsumerTopologyDescriptor
{
    private NatsConsumerTopologyDescriptor(IMessagingConfigurationContext context, string name)
        : base(context)
    {
        Configuration = new NatsConsumerConfiguration
        {
            Name = name,
            FilterSubjects = [],
            Origin = TopologyOrigin.Declared
        };
    }

    /// <inheritdoc />
    protected internal override NatsConsumerConfiguration Configuration { get; protected set; }

    /// <summary>
    /// Creates a descriptor for a consumer with the specified durable name.
    /// </summary>
    /// <param name="context">The configuration context.</param>
    /// <param name="name">The durable consumer name.</param>
    /// <returns>The new descriptor.</returns>
    public static NatsConsumerTopologyDescriptor New(IMessagingConfigurationContext context, string name)
        => new(context, name);

    /// <summary>
    /// Gets the configuration this descriptor has built.
    /// </summary>
    /// <returns>The consumer configuration.</returns>
    public NatsConsumerConfiguration CreateConfiguration() => Configuration;

    /// <inheritdoc />
    public INatsConsumerTopologyDescriptor Subject(string subject)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);

        Configuration.FilterSubjects ??= [];

        if (!Configuration.FilterSubjects.Contains(subject))
        {
            Configuration.FilterSubjects.Add(subject);
        }

        return this;
    }

    /// <inheritdoc />
    public INatsConsumerTopologyDescriptor FromStream(string streamName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(streamName);

        Configuration.StreamName = streamName;
        return this;
    }

    /// <inheritdoc />
    public INatsConsumerTopologyDescriptor AckWait(TimeSpan ackWait)
    {
        Configuration.AckWait = ackWait;
        return this;
    }

    /// <inheritdoc />
    public INatsConsumerTopologyDescriptor MaxAckPending(long maxAckPending)
    {
        Configuration.MaxAckPending = maxAckPending;
        return this;
    }

    /// <inheritdoc />
    public INatsConsumerTopologyDescriptor MaxDeliver(long maxDeliver)
    {
        Configuration.MaxDeliver = maxDeliver;
        return this;
    }

    /// <inheritdoc />
    public INatsConsumerTopologyDescriptor Backoff(params TimeSpan[] backoff)
    {
        Configuration.Backoff = [.. backoff];
        return this;
    }

    /// <inheritdoc />
    public INatsConsumerTopologyDescriptor AckProgressEvery(TimeSpan interval)
    {
        Configuration.AckProgressInterval = interval;
        return this;
    }

    /// <inheritdoc />
    public INatsConsumerTopologyDescriptor DeliverFrom(ConsumerConfigDeliverPolicy deliverPolicy)
    {
        Configuration.DeliverPolicy = deliverPolicy;
        return this;
    }

    /// <inheritdoc />
    public INatsConsumerTopologyDescriptor AutoProvision(bool autoProvision = true)
    {
        Configuration.AutoProvision = autoProvision;
        return this;
    }
}
