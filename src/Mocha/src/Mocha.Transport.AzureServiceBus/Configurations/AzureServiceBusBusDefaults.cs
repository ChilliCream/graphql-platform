namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Defines bus-level defaults that are applied to all auto-provisioned queues, topics,
/// and endpoints when they are created by topology conventions.
/// </summary>
public sealed class AzureServiceBusBusDefaults
{
    /// <summary>
    /// Gets or sets the default queue configuration that is applied to all auto-provisioned queues.
    /// Individual queue settings will override these defaults.
    /// </summary>
    public AzureServiceBusDefaultQueueOptions Queue { get; set; } = new();

    /// <summary>
    /// Gets or sets the default topic configuration that is applied to all auto-provisioned topics.
    /// Individual topic settings will override these defaults.
    /// </summary>
    public AzureServiceBusDefaultTopicOptions Topic { get; set; } = new();

    /// <summary>
    /// Gets or sets the default receive endpoint configuration that is applied to all auto-provisioned endpoints.
    /// Individual endpoint settings will override these defaults.
    /// </summary>
    public AzureServiceBusDefaultEndpointOptions Endpoint { get; set; } = new();
}
