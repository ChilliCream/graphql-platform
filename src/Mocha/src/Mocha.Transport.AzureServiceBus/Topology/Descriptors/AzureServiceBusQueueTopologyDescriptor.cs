namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Descriptor implementation for configuring an Azure Service Bus queue.
/// </summary>
internal sealed class AzureServiceBusQueueTopologyDescriptor
    : MessagingDescriptorBase<AzureServiceBusQueueConfiguration>
    , IAzureServiceBusQueueTopologyDescriptor
{
    public AzureServiceBusQueueTopologyDescriptor(IMessagingConfigurationContext context, string name) : base(context)
    {
        Configuration = new AzureServiceBusQueueConfiguration
        {
            Name = name,
            Origin = TopologyOrigin.Declared
        };
    }

    /// <inheritdoc />
    protected internal override AzureServiceBusQueueConfiguration Configuration { get; protected set; }

    /// <inheritdoc />
    public IAzureServiceBusQueueTopologyDescriptor Name(string name)
    {
        Configuration.Name = name;
        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusQueueTopologyDescriptor AutoDelete(bool autoDelete = true)
    {
        Configuration.AutoDelete = autoDelete;
        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusQueueTopologyDescriptor AutoProvision(bool autoProvision = true)
    {
        Configuration.AutoProvision = autoProvision;
        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusQueueTopologyDescriptor WithAutoDeleteOnIdle(TimeSpan autoDeleteOnIdle)
    {
        Configuration.AutoDeleteOnIdle = autoDeleteOnIdle;
        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusQueueTopologyDescriptor WithLockDuration(TimeSpan lockDuration)
    {
        Configuration.LockDuration = lockDuration;
        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusQueueTopologyDescriptor WithMaxDeliveryCount(int maxDeliveryCount)
    {
        Configuration.MaxDeliveryCount = maxDeliveryCount;
        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusQueueTopologyDescriptor WithDefaultMessageTimeToLive(TimeSpan defaultMessageTimeToLive)
    {
        Configuration.DefaultMessageTimeToLive = defaultMessageTimeToLive;
        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusQueueTopologyDescriptor WithMaxSizeInMegabytes(long maxSizeInMegabytes)
    {
        Configuration.MaxSizeInMegabytes = maxSizeInMegabytes;
        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusQueueTopologyDescriptor WithRequiresSession(bool requiresSession = true)
    {
        Configuration.RequiresSession = requiresSession;
        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusQueueTopologyDescriptor WithEnablePartitioning(bool enablePartitioning = true)
    {
        Configuration.EnablePartitioning = enablePartitioning;
        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusQueueTopologyDescriptor WithForwardTo(string entityName)
    {
        Configuration.ForwardTo = entityName;
        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusQueueTopologyDescriptor WithForwardDeadLetteredMessagesTo(string entityName)
    {
        Configuration.ForwardDeadLetteredMessagesTo = entityName;
        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusQueueTopologyDescriptor WithDeadLetteringOnMessageExpiration(
        bool deadLetteringOnMessageExpiration = true)
    {
        Configuration.DeadLetteringOnMessageExpiration = deadLetteringOnMessageExpiration;
        return this;
    }

    /// <summary>
    /// Creates the final queue configuration.
    /// </summary>
    /// <returns>The configured queue configuration.</returns>
    public AzureServiceBusQueueConfiguration CreateConfiguration() => Configuration;

    /// <summary>
    /// Creates a new queue topology descriptor with the specified name.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="name">The queue name.</param>
    /// <returns>A new queue descriptor.</returns>
    public static AzureServiceBusQueueTopologyDescriptor New(IMessagingConfigurationContext context, string name)
        => new(context, name);
}
