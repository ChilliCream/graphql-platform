namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Fluent interface for configuring an Azure Service Bus topic.
/// </summary>
public interface IAzureServiceBusTopicDescriptor : IMessagingDescriptor<AzureServiceBusTopicConfiguration>
{
    /// <summary>
    /// Sets the name of the topic.
    /// </summary>
    /// <param name="name">The topic name.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IAzureServiceBusTopicDescriptor Name(string name);

    /// <summary>
    /// Sets whether the topic should be automatically provisioned.
    /// </summary>
    /// <param name="autoProvision">True to enable auto-provisioning (default: true).</param>
    /// <returns>The descriptor for method chaining.</returns>
    IAzureServiceBusTopicDescriptor AutoProvision(bool autoProvision = true);
}
