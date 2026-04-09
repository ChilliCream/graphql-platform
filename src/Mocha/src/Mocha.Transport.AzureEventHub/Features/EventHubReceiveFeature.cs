using Azure.Messaging.EventHubs;
using Mocha.Features;

namespace Mocha.Transport.AzureEventHub.Features;

/// <summary>
/// Pooled feature that carries the Event Hub <see cref="EventData"/> and partition ID through the receive
/// middleware pipeline, enabling parsing and acknowledgement middleware to access the raw event context.
/// </summary>
public sealed class EventHubReceiveFeature : IPooledFeature
{
    /// <summary>
    /// Gets or sets the raw Event Hub event data for the current message.
    /// </summary>
    public EventData EventData { get; set; } = null!;

    /// <summary>
    /// Gets or sets the partition ID from which this event was received.
    /// </summary>
    public string PartitionId { get; set; } = null!;

    /// <summary>
    /// Gets or sets the sequence number assigned by Event Hubs when the event was enqueued.
    /// </summary>
    public long SequenceNumber { get; set; }

    /// <summary>
    /// Gets or sets the UTC time at which the event was enqueued in Event Hubs.
    /// </summary>
    public DateTimeOffset EnqueuedTime { get; set; }

    /// <inheritdoc />
    public void Initialize(object state)
    {
        EventData = null!;
        PartitionId = null!;
        SequenceNumber = 0;
        EnqueuedTime = default;
    }

    /// <inheritdoc />
    public void Reset()
    {
        EventData = null!;
        PartitionId = null!;
        SequenceNumber = 0;
        EnqueuedTime = default;
    }
}
