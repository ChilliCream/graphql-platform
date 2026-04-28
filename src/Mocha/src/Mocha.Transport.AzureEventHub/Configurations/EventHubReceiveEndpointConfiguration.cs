using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Primitives;

namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Configuration for an Event Hub receive endpoint, specifying the source hub and consumer group.
/// </summary>
public sealed class EventHubReceiveEndpointConfiguration : ReceiveEndpointConfiguration
{
    /// <summary>
    /// Gets or sets the Event Hub name from which this endpoint consumes messages.
    /// </summary>
    public string? HubName { get; set; }

    /// <summary>
    /// Gets or sets the consumer group used by this endpoint.
    /// Defaults to "$Default".
    /// </summary>
    public string ConsumerGroup { get; set; } = "$Default";

    /// <summary>
    /// Gets or sets the number of events processed between checkpoints.
    /// Defaults to 100. Set to 1 to checkpoint after every event.
    /// </summary>
    public int CheckpointInterval { get; set; } = 100;

    /// <summary>
    /// Gets or sets the starting position used when no checkpoint exists for a partition.
    /// Defaults to <see cref="EventPosition.Earliest"/> to prevent silent data loss on first deploy.
    /// </summary>
    public EventPosition DefaultStartingPosition { get; set; } = EventPosition.Earliest;

    /// <summary>
    /// Gets or sets the maximum number of events per batch delivered to the processor.
    /// Defaults to 100. Set to 1 for single-event processing.
    /// </summary>
    public int EventBatchMaximumCount { get; set; } = 100;

    /// <summary>
    /// Gets or sets optional processor options controlling load balancing, prefetch,
    /// retry, and other <see cref="EventProcessor{TPartition}"/> behaviors.
    /// When <c>null</c>, the SDK defaults are used.
    /// </summary>
    public EventProcessorOptions? ProcessorOptions { get; set; }
}
