namespace Mocha.Transport.NATS;

/// <summary>
/// Fluent interface for configuring a NATS JetStream durable consumer.
/// </summary>
public interface INatsConsumerDescriptor : IMessagingDescriptor<NatsConsumerConfiguration>
{
    /// <summary>
    /// Sets the durable name of the consumer.
    /// </summary>
    /// <param name="name">The consumer name.</param>
    /// <returns>The descriptor for method chaining.</returns>
    INatsConsumerDescriptor Name(string name);

    /// <summary>
    /// Sets the stream that this consumer is bound to.
    /// </summary>
    /// <param name="streamName">The stream name.</param>
    /// <returns>The descriptor for method chaining.</returns>
    INatsConsumerDescriptor Stream(string streamName);

    /// <summary>
    /// Sets the filter subject for this consumer.
    /// </summary>
    /// <param name="filterSubject">The subject pattern to filter on.</param>
    /// <returns>The descriptor for method chaining.</returns>
    INatsConsumerDescriptor FilterSubject(string filterSubject);

    /// <summary>
    /// Sets the maximum number of unacknowledged messages.
    /// </summary>
    /// <param name="maxAckPending">The max ack pending count.</param>
    /// <returns>The descriptor for method chaining.</returns>
    INatsConsumerDescriptor MaxAckPending(int maxAckPending);

    /// <summary>
    /// Sets the acknowledgment wait timeout.
    /// </summary>
    /// <param name="ackWait">The ack wait duration.</param>
    /// <returns>The descriptor for method chaining.</returns>
    INatsConsumerDescriptor AckWait(TimeSpan ackWait);

    /// <summary>
    /// Sets the maximum number of delivery attempts before the message is terminated.
    /// </summary>
    /// <param name="maxDeliver">The maximum delivery count.</param>
    /// <returns>The descriptor for method chaining.</returns>
    INatsConsumerDescriptor MaxDeliver(int maxDeliver);

    /// <summary>
    /// Sets whether the consumer should be automatically provisioned.
    /// </summary>
    /// <param name="autoProvision">True to enable auto-provisioning (default: true).</param>
    /// <returns>The descriptor for method chaining.</returns>
    INatsConsumerDescriptor AutoProvision(bool autoProvision = true);
}
