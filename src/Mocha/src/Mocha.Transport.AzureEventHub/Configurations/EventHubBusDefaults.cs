namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Defines bus-level defaults that are applied to all auto-provisioned topics and subscriptions
/// when they are created by topology conventions.
/// </summary>
public sealed class EventHubBusDefaults
{
    /// <summary>
    /// Gets or sets the default topic configuration that is applied to all auto-provisioned topics.
    /// Individual topic settings will override these defaults.
    /// </summary>
    public EventHubDefaultTopicOptions Topic { get; set; } = new();

    /// <summary>
    /// Gets or sets the default subscription configuration that is applied to all auto-provisioned subscriptions.
    /// Individual subscription settings will override these defaults.
    /// </summary>
    public EventHubDefaultSubscriptionOptions Subscription { get; set; } = new();

    /// <summary>
    /// Gets or sets the default batch mode applied to all dispatch endpoints that do not
    /// specify an explicit batch mode. Defaults to <see cref="EventHubBatchMode.Single"/>.
    /// </summary>
    public EventHubBatchMode DefaultBatchMode { get; set; } = EventHubBatchMode.Single;
}
