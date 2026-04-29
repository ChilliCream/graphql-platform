namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Descriptor implementation for configuring an Azure Service Bus subscription (topic-to-queue forwarding).
/// </summary>
internal sealed class AzureServiceBusSubscriptionDescriptor
    : MessagingDescriptorBase<AzureServiceBusSubscriptionConfiguration>
    , IAzureServiceBusSubscriptionDescriptor
{
    public AzureServiceBusSubscriptionDescriptor(
        IMessagingConfigurationContext context,
        string source,
        string destination) : base(context)
    {
        Configuration = new AzureServiceBusSubscriptionConfiguration { Source = source, Destination = destination };
    }

    /// <inheritdoc />
    protected internal override AzureServiceBusSubscriptionConfiguration Configuration { get; protected set; }

    /// <inheritdoc />
    public IAzureServiceBusSubscriptionDescriptor AutoProvision(bool autoProvision = true)
    {
        Configuration.AutoProvision = autoProvision;
        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusSubscriptionDescriptor WithLockDuration(TimeSpan lockDuration)
    {
        Configuration.LockDuration = lockDuration;
        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusSubscriptionDescriptor WithMaxDeliveryCount(int maxDeliveryCount)
    {
        Configuration.MaxDeliveryCount = maxDeliveryCount;
        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusSubscriptionDescriptor WithDefaultMessageTimeToLive(TimeSpan defaultMessageTimeToLive)
    {
        Configuration.DefaultMessageTimeToLive = defaultMessageTimeToLive;
        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusSubscriptionDescriptor WithRequiresSession(bool requiresSession = true)
    {
        Configuration.RequiresSession = requiresSession;
        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusSubscriptionDescriptor WithForwardTo(string entityName)
    {
        Configuration.ForwardTo = entityName;
        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusSubscriptionDescriptor WithForwardDeadLetteredMessagesTo(string entityName)
    {
        Configuration.ForwardDeadLetteredMessagesTo = entityName;
        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusSubscriptionDescriptor WithDeadLetteringOnMessageExpiration(bool deadLetteringOnMessageExpiration = true)
    {
        Configuration.DeadLetteringOnMessageExpiration = deadLetteringOnMessageExpiration;
        return this;
    }

    /// <inheritdoc />
    public IAzureServiceBusSubscriptionDescriptor WithAutoDeleteOnIdle(TimeSpan autoDeleteOnIdle)
    {
        Configuration.AutoDeleteOnIdle = autoDeleteOnIdle;
        return this;
    }

    /// <summary>
    /// Creates the final subscription configuration.
    /// </summary>
    /// <returns>The configured subscription configuration.</returns>
    public AzureServiceBusSubscriptionConfiguration CreateConfiguration() => Configuration;

    /// <summary>
    /// Creates a new subscription descriptor with the specified source and destination.
    /// </summary>
    /// <param name="context">The messaging configuration context.</param>
    /// <param name="source">The source topic name.</param>
    /// <param name="destination">The destination queue name.</param>
    /// <returns>A new subscription descriptor.</returns>
    public static AzureServiceBusSubscriptionDescriptor New(
        IMessagingConfigurationContext context,
        string source,
        string destination)
        => new(context, source, destination);
}
