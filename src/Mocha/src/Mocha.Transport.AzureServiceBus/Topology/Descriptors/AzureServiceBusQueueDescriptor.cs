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

    /// <inheritdoc />
    public IAzureServiceBusQueueDescriptor WithAutoDeleteOnIdle(TimeSpan autoDeleteOnIdle)
    {
        Configuration.AutoDeleteOnIdle = autoDeleteOnIdle;
        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusQueueDescriptor WithLockDuration(TimeSpan lockDuration)
    {
        Configuration.LockDuration = lockDuration;
        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusQueueDescriptor WithMaxDeliveryCount(int maxDeliveryCount)
    {
        Configuration.MaxDeliveryCount = maxDeliveryCount;
        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusQueueDescriptor WithDefaultMessageTimeToLive(TimeSpan defaultMessageTimeToLive)
    {
        Configuration.DefaultMessageTimeToLive = defaultMessageTimeToLive;
        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusQueueDescriptor WithMaxSizeInMegabytes(long maxSizeInMegabytes)
    {
        Configuration.MaxSizeInMegabytes = maxSizeInMegabytes;
        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusQueueDescriptor WithRequiresSession(bool requiresSession = true)
    {
        Configuration.RequiresSession = requiresSession;
        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusQueueDescriptor WithEnablePartitioning(bool enablePartitioning = true)
    {
        Configuration.EnablePartitioning = enablePartitioning;
        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusQueueDescriptor WithForwardTo(string entityName)
    {
        Configuration.ForwardTo = entityName;
        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusQueueDescriptor WithForwardDeadLetteredMessagesTo(string entityName)
    {
        Configuration.ForwardDeadLetteredMessagesTo = entityName;
        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusQueueDescriptor WithDeadLetteringOnMessageExpiration(bool deadLetteringOnMessageExpiration = true)
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
    /// Creates a new queue descriptor with the specified name.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="name">The queue name.</param>
    /// <returns>A new queue descriptor.</returns>
    public static AzureServiceBusQueueDescriptor New(IMessagingConfigurationContext context, string name)
        => new(context, name);
}
