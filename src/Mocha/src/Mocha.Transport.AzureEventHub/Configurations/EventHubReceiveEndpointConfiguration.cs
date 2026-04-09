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
}
