namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Default options for subscriptions (consumer groups) created by topology conventions.
/// </summary>
public sealed class EventHubDefaultSubscriptionOptions
{
    /// <summary>
    /// Applies these defaults to a subscription configuration, without overriding explicitly set values.
    /// </summary>
    internal void ApplyTo(EventHubSubscriptionConfiguration configuration)
    {
        // No defaults to apply currently. Placeholder for future subscription-level defaults
        // (e.g., default checkpoint interval, default consumer group naming).
    }
}
