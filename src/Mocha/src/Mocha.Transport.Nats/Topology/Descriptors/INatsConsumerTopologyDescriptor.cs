using NATS.Client.JetStream.Models;

namespace Mocha.Transport.Nats;

/// <summary>
/// Fluent descriptor for declaring a durable JetStream pull consumer.
/// </summary>
public interface INatsConsumerTopologyDescriptor : IMessagingDescriptor<NatsConsumerConfiguration>
{
    /// <summary>
    /// Adds a subject this consumer receives.
    /// </summary>
    /// <param name="subject">The subject or wildcard filter.</param>
    /// <returns>The descriptor for method chaining.</returns>
    INatsConsumerTopologyDescriptor Subject(string subject);

    /// <summary>
    /// Pins this consumer to a specific stream instead of resolving the owning stream at start-up.
    /// </summary>
    /// <param name="streamName">The stream name.</param>
    /// <returns>The descriptor for method chaining.</returns>
    /// <remarks>
    /// Declaring the stream also makes start-up order irrelevant, because whichever service starts
    /// first provisions it.
    /// </remarks>
    INatsConsumerTopologyDescriptor FromStream(string streamName);

    /// <summary>
    /// Sets how long the server waits for an acknowledgement before redelivering.
    /// </summary>
    /// <param name="ackWait">The acknowledgement deadline.</param>
    /// <returns>The descriptor for method chaining.</returns>
    INatsConsumerTopologyDescriptor AckWait(TimeSpan ackWait);

    /// <summary>
    /// Sets the maximum number of unacknowledged messages in flight.
    /// </summary>
    /// <param name="maxAckPending">The in-flight limit.</param>
    /// <returns>The descriptor for method chaining.</returns>
    INatsConsumerTopologyDescriptor MaxAckPending(long maxAckPending);

    /// <summary>
    /// Sets the broker-side delivery limit.
    /// </summary>
    /// <param name="maxDeliver">The maximum number of delivery attempts.</param>
    /// <returns>The descriptor for method chaining.</returns>
    /// <remarks>
    /// Mocha owns the retry decision, so this should sit above Mocha's own retry policy and act as
    /// a safety net rather than the primary limit.
    /// </remarks>
    INatsConsumerTopologyDescriptor MaxDeliver(long maxDeliver);

    /// <summary>
    /// Sets the per-attempt redelivery backoff curve.
    /// </summary>
    /// <param name="backoff">The delay before each successive redelivery.</param>
    /// <returns>The descriptor for method chaining.</returns>
    INatsConsumerTopologyDescriptor Backoff(params TimeSpan[] backoff);

    /// <summary>
    /// Reports progress on an in-flight message at this interval, extending its acknowledgement
    /// deadline for as long as the handler is still running.
    /// </summary>
    /// <param name="interval">The reporting interval, typically a fraction of the acknowledgement deadline.</param>
    /// <returns>The descriptor for method chaining.</returns>
    /// <remarks>
    /// Off by default because it costs a background task per in-flight message. Enable it for
    /// handlers that can legitimately run longer than the acknowledgement deadline, which would
    /// otherwise be redelivered while still being processed.
    /// </remarks>
    INatsConsumerTopologyDescriptor AckProgressEvery(TimeSpan interval);

    /// <summary>
    /// Sets where a newly created consumer starts reading. Has no effect once the consumer exists.
    /// </summary>
    /// <param name="deliverPolicy">The delivery policy.</param>
    /// <returns>The descriptor for method chaining.</returns>
    INatsConsumerTopologyDescriptor DeliverFrom(ConsumerConfigDeliverPolicy deliverPolicy);

    /// <summary>
    /// Controls whether this consumer is provisioned during start-up.
    /// </summary>
    /// <param name="autoProvision">Whether to provision the consumer.</param>
    /// <returns>The descriptor for method chaining.</returns>
    INatsConsumerTopologyDescriptor AutoProvision(bool autoProvision = true);
}
