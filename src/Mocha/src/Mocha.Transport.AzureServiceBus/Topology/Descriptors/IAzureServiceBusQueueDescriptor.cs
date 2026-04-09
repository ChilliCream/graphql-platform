namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Fluent interface for configuring an Azure Service Bus queue.
/// </summary>
public interface IAzureServiceBusQueueDescriptor : IMessagingDescriptor<AzureServiceBusQueueConfiguration>
{
    /// <summary>
    /// Sets the name of the queue.
    /// </summary>
    /// <param name="name">The queue name.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IAzureServiceBusQueueDescriptor Name(string name);

    /// <summary>
    /// Sets whether the queue is automatically deleted when no longer in use.
    /// </summary>
    /// <param name="autoDelete">True to enable auto-deletion (default: true).</param>
    /// <returns>The descriptor for method chaining.</returns>
    IAzureServiceBusQueueDescriptor AutoDelete(bool autoDelete = true);

    /// <summary>
    /// Sets whether the queue should be automatically provisioned.
    /// </summary>
    /// <param name="autoProvision">True to enable auto-provisioning (default: true).</param>
    /// <returns>The descriptor for method chaining.</returns>
    IAzureServiceBusQueueDescriptor AutoProvision(bool autoProvision = true);
}
