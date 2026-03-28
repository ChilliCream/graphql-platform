using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace Mocha.Transport.NATS;

/// <summary>
/// Represents a NATS JetStream durable consumer topology resource.
/// </summary>
public sealed class NatsConsumer : TopologyResource<NatsConsumerConfiguration>, INatsResource
{
    /// <summary>
    /// Gets the durable name of this consumer.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Gets the name of the stream this consumer is bound to.
    /// </summary>
    public string? StreamName { get; private set; }

    /// <summary>
    /// Gets the filter subject for this consumer.
    /// </summary>
    public string? FilterSubject { get; private set; }

    /// <summary>
    /// Gets the maximum number of unacknowledged messages.
    /// </summary>
    public int? MaxAckPending { get; private set; }

    /// <summary>
    /// Gets the acknowledgment wait timeout.
    /// </summary>
    public TimeSpan? AckWait { get; private set; }

    /// <summary>
    /// Gets the maximum number of delivery attempts before the message is terminated.
    /// When <c>null</c>, defaults to 5 during provisioning.
    /// </summary>
    public int? MaxDeliver { get; private set; }

    /// <summary>
    /// Gets a value indicating whether this consumer is automatically provisioned during topology setup.
    /// When <c>null</c>, the transport-level default is used.
    /// </summary>
    public bool? AutoProvision { get; private set; }

    protected override void OnInitialize(NatsConsumerConfiguration configuration)
    {
        Name = configuration.Name;
        StreamName = configuration.StreamName;
        FilterSubject = configuration.FilterSubject;
        MaxAckPending = configuration.MaxAckPending;
        AckWait = configuration.AckWait;
        MaxDeliver = configuration.MaxDeliver;
        AutoProvision = configuration.AutoProvision;
    }

    protected override void OnComplete(NatsConsumerConfiguration configuration)
    {
        var address = new UriBuilder(Topology.Address);
        address.Path = Path.Combine(address.Path, "c", configuration.Name);
        Address = address.Uri;
    }

    /// <summary>
    /// Creates or updates this consumer on the NATS server.
    /// </summary>
    /// <param name="js">The JetStream context to use.</param>
    /// <param name="streamName">The stream name to bind the consumer to.</param>
    /// <param name="cancellationToken">A token to cancel the provisioning operation.</param>
    public async Task ProvisionAsync(INatsJSContext js, string streamName, CancellationToken cancellationToken)
    {
        var config = new ConsumerConfig(Name)
        {
            DurableName = Name,
            AckPolicy = ConsumerConfigAckPolicy.Explicit,
            DeliverPolicy = ConsumerConfigDeliverPolicy.All,
            MaxDeliver = MaxDeliver ?? 5
        };

        if (FilterSubject is not null)
        {
            config.FilterSubject = FilterSubject;
        }

        if (MaxAckPending.HasValue)
        {
            config.MaxAckPending = MaxAckPending.Value;
        }

        if (AckWait.HasValue)
        {
            config.AckWait = AckWait.Value;
        }

        await js.CreateOrUpdateConsumerAsync(streamName, config, cancellationToken);
    }

    /// <summary>
    /// Provisions this consumer using the stored stream name.
    /// </summary>
    /// <param name="js">The JetStream context to use.</param>
    /// <param name="cancellationToken">A token to cancel the provisioning operation.</param>
    public Task ProvisionAsync(INatsJSContext js, CancellationToken cancellationToken)
    {
        if (StreamName is null)
        {
            throw new InvalidOperationException("Stream name is required to provision a consumer");
        }

        return ProvisionAsync(js, StreamName, cancellationToken);
    }
}
