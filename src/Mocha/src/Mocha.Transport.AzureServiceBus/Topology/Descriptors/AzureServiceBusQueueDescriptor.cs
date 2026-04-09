namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Descriptor implementation for configuring an Azure Service Bus queue.
/// </summary>
internal sealed class AzureServiceBusQueueDescriptor
    : MessagingDescriptorBase<AzureServiceBusQueueConfiguration>
    , IAzureServiceBusQueueDescriptor
{
    public AzureServiceBusQueueDescriptor(IMessagingConfigurationContext context, string name) : base(context)
    {
        Configuration = new AzureServiceBusQueueConfiguration { Name = name };
    }

    /// <inheritdoc />
    protected internal override AzureServiceBusQueueConfiguration Configuration { get; protected set; }

    /// <inheritdoc />
    public IAzureServiceBusQueueDescriptor Name(string name)
    {
        Configuration.Name = name;
        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusQueueDescriptor AutoDelete(bool autoDelete = true)
    {
        Configuration.AutoDelete = autoDelete;
        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusQueueDescriptor AutoProvision(bool autoProvision = true)
    {
        Configuration.AutoProvision = autoProvision;
        return this;
    }

    /// <summary>
    /// Creates the final queue configuration.
    /// </summary>
    /// <returns>The configured queue configuration.</returns>
    public AzureServiceBusQueueConfiguration CreateConfiguration() => Configuration;

    /// <summary>
    /// Creates a new queue descriptor with the specified name.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="name">The queue name.</param>
    /// <returns>A new queue descriptor.</returns>
    public static AzureServiceBusQueueDescriptor New(IMessagingConfigurationContext context, string name)
        => new(context, name);
}
