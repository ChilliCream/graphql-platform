namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Default options for topics (Event Hub entities) created by topology conventions.
/// </summary>
public sealed class EventHubDefaultTopicOptions
{
    /// <summary>
    /// Gets or sets the default partition count for auto-provisioned topics.
    /// When <c>null</c>, the Azure default is used.
    /// </summary>
    public int? PartitionCount { get; set; }

    /// <summary>
    /// Applies these defaults to a topic configuration, without overriding explicitly set values.
    /// </summary>
    internal void ApplyTo(EventHubTopicConfiguration configuration)
    {
        configuration.PartitionCount ??= PartitionCount;
    }
}
