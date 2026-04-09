namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Fluent interface for configuring an Event Hub subscription (consumer group).
/// </summary>
public interface IEventHubSubscriptionDescriptor : IMessagingDescriptor<EventHubSubscriptionConfiguration>
{
    /// <summary>
    /// Sets whether the consumer group should be automatically provisioned.
    /// </summary>
    /// <param name="autoProvision"><c>true</c> to enable auto-provisioning (default).</param>
    /// <returns>The descriptor for method chaining.</returns>
    IEventHubSubscriptionDescriptor AutoProvision(bool autoProvision = true);
}
