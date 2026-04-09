namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Configuration for an Event Hub dispatch endpoint, specifying the target hub for outbound messages.
/// </summary>
public sealed class EventHubDispatchEndpointConfiguration : DispatchEndpointConfiguration
{
    /// <summary>
    /// Gets or sets the target Event Hub name for outbound message dispatch.
    /// </summary>
    public string? HubName { get; set; }

    /// <summary>
    /// Gets or sets the static partition ID for outbound message dispatch.
    /// When set, all messages from this endpoint are sent to the specified partition.
    /// </summary>
    public string? PartitionId { get; set; }

    /// <summary>
    /// Gets or sets the batch mode for this dispatch endpoint.
    /// When <c>null</c>, the bus-level <see cref="EventHubBusDefaults.DefaultBatchMode"/> is used.
    /// </summary>
    public EventHubBatchMode? BatchMode { get; set; }
}
