namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Configures an Azure Service Bus queue together with its receive endpoint.
/// </summary>
public interface IAzureServiceBusQueueDescriptor : IMessagingDescriptor<AzureServiceBusQueueDescriptorConfiguration>
{
    IAzureServiceBusQueueDescriptor AutoProvision(bool autoProvision = true);

    /// <summary>
    /// Configures the Azure Service Bus idle deletion policy.
    /// </summary>
    IAzureServiceBusQueueDescriptor AutoDeleteOnIdle(TimeSpan autoDeleteOnIdle);

    IAzureServiceBusQueueDescriptor LockDuration(TimeSpan lockDuration);

    IAzureServiceBusQueueDescriptor MaxDeliveryCount(int maxDeliveryCount);

    IAzureServiceBusQueueDescriptor DefaultMessageTimeToLive(TimeSpan defaultMessageTimeToLive);

    IAzureServiceBusQueueDescriptor MaxSizeInMegabytes(long maxSizeInMegabytes);

    IAzureServiceBusQueueDescriptor RequiresSession(bool requiresSession = true);

    IAzureServiceBusQueueDescriptor EnablePartitioning(bool enablePartitioning = true);

    IAzureServiceBusQueueDescriptor ForwardTo(string entityName);

    IAzureServiceBusQueueDescriptor ForwardDeadLetteredMessagesTo(string entityName);

    IAzureServiceBusQueueDescriptor DeadLetteringOnMessageExpiration(
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

    IAzureServiceBusQueueDescriptor MaxConcurrentSessions(int maxConcurrentSessions);

    IAzureServiceBusQueueDescriptor MaxConcurrentCallsPerSession(int maxConcurrentCallsPerSession);

    IAzureServiceBusQueueDescriptor SessionIdleTimeout(TimeSpan sessionIdleTimeout);

    IAzureServiceBusQueueDescriptor MaxAutoLockRenewalDuration(TimeSpan maxAutoLockRenewalDuration);

    /// <summary>
    /// Declares an explicit topic from which this queue receives messages.
    /// </summary>
    IAzureServiceBusQueueDescriptor BindFrom(Uri source);
}
