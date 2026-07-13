namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Configures an Azure Service Bus queue together with its receive endpoint.
/// </summary>
public interface IAzureServiceBusQueueDescriptor : IMessagingDescriptor<AzureServiceBusQueueDescriptorConfiguration>
{
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

    IAzureServiceBusQueueDescriptor DisableFaultEndpoint();

    IAzureServiceBusQueueDescriptor SkippedEndpoint(Uri address);

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
