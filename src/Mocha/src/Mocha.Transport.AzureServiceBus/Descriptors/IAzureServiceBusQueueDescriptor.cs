namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Configures an Azure Service Bus queue together with its receive endpoint.
/// </summary>
public interface IAzureServiceBusQueueDescriptor : IMessagingDescriptor<AzureServiceBusQueueDescriptorConfiguration>
{
    /// <summary>
    /// Compatibility toggle for <see cref="WithAutoDeleteOnIdle"/>. <c>true</c> alone configures
    /// no broker option, while <c>false</c> suppresses a configured idle deletion policy.
    /// </summary>
    IAzureServiceBusQueueDescriptor AutoDelete(bool autoDelete = true);

    IAzureServiceBusQueueDescriptor AutoProvision(bool autoProvision = true);

    /// <summary>Configures the Azure Service Bus idle deletion policy.</summary>
    IAzureServiceBusQueueDescriptor WithAutoDeleteOnIdle(TimeSpan autoDeleteOnIdle);

    IAzureServiceBusQueueDescriptor WithLockDuration(TimeSpan lockDuration);

    IAzureServiceBusQueueDescriptor WithMaxDeliveryCount(int maxDeliveryCount);

    IAzureServiceBusQueueDescriptor WithDefaultMessageTimeToLive(TimeSpan defaultMessageTimeToLive);

    IAzureServiceBusQueueDescriptor WithMaxSizeInMegabytes(long maxSizeInMegabytes);

    IAzureServiceBusQueueDescriptor WithRequiresSession(bool requiresSession = true);

    IAzureServiceBusQueueDescriptor WithEnablePartitioning(bool enablePartitioning = true);

    IAzureServiceBusQueueDescriptor WithForwardTo(string entityName);

    IAzureServiceBusQueueDescriptor WithForwardDeadLetteredMessagesTo(string entityName);

    IAzureServiceBusQueueDescriptor WithDeadLetteringOnMessageExpiration(
        bool deadLetteringOnMessageExpiration = true);

    IAzureServiceBusQueueDescriptor Handler<THandler>() where THandler : class, IHandler;

    IAzureServiceBusQueueDescriptor Handler(Type handlerType);

    IAzureServiceBusQueueDescriptor Consumer<TConsumer>() where TConsumer : class, IConsumer;

    IAzureServiceBusQueueDescriptor Consumer(Type consumerType);

    IAzureServiceBusQueueDescriptor Receives<TMessage>();

    IAzureServiceBusQueueDescriptor Receives(Type messageType);

    IAzureServiceBusQueueDescriptor BindImplicitly();

    IAzureServiceBusQueueDescriptor BindExplicitly();

    IAzureServiceBusQueueDescriptor Kind(ReceiveEndpointKind kind);

    IAzureServiceBusQueueDescriptor MaxConcurrency(int maxConcurrency);

    IAzureServiceBusQueueDescriptor PrefetchCount(int? count);

    IAzureServiceBusQueueDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null);

    IAzureServiceBusQueueDescriptor FaultEndpoint(Uri address);

    [Obsolete("Use FaultEndpoint(Uri) instead.")]
    IAzureServiceBusQueueDescriptor FaultEndpoint(string address);

    IAzureServiceBusQueueDescriptor DisableFaultEndpoint();

    IAzureServiceBusQueueDescriptor SkippedEndpoint(Uri address);

    [Obsolete("Use SkippedEndpoint(Uri) instead.")]
    IAzureServiceBusQueueDescriptor SkippedEndpoint(string address);

    IAzureServiceBusQueueDescriptor DisableSkippedEndpoint();

    IAzureServiceBusQueueDescriptor UseNativeDeadLetterForwarding();

    IAzureServiceBusQueueDescriptor WithMaxConcurrentSessions(int maxConcurrentSessions);

    IAzureServiceBusQueueDescriptor WithMaxConcurrentCallsPerSession(int maxConcurrentCallsPerSession);

    IAzureServiceBusQueueDescriptor WithSessionIdleTimeout(TimeSpan sessionIdleTimeout);

    IAzureServiceBusQueueDescriptor WithMaxAutoLockRenewalDuration(TimeSpan maxAutoLockRenewalDuration);

    /// <summary>
    /// Declares an explicit topic from which this queue receives messages.
    /// </summary>
    IAzureServiceBusQueueDescriptor BindFrom(Uri source);
}
