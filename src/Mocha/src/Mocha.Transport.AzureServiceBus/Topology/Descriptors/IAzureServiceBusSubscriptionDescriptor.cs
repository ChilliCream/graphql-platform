namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Fluent interface for configuring an Azure Service Bus subscription (topic-to-queue forwarding).
/// </summary>
public interface IAzureServiceBusSubscriptionDescriptor : IMessagingDescriptor<AzureServiceBusSubscriptionConfiguration>
{
    /// <summary>
    /// Sets whether the subscription should be automatically provisioned.
    /// </summary>
    /// <param name="autoProvision">True to enable auto-provisioning (default: true).</param>
    /// <returns>The descriptor for method chaining.</returns>
    IAzureServiceBusSubscriptionDescriptor AutoProvision(bool autoProvision = true);
}
