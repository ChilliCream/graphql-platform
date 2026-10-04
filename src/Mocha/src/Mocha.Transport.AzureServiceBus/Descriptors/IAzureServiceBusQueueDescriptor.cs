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

    /// <summary>
    /// Marks this queue's receive endpoint as temporary, using the default idle window.
    /// </summary>
    /// <returns>The descriptor for method chaining.</returns>
    IAzureServiceBusQueueDescriptor Temporary();

    /// <summary>
    /// Marks this queue's receive endpoint as temporary with an explicit idle window after which
    /// the broker may delete the queue.
    /// </summary>
    /// <param name="idleTimeout">
    /// The idle window. Must be at least <see cref="AzureServiceBusReceiveEndpointConfiguration.TemporaryDefaults.MinimumAutoDeleteOnIdle"/>.
    /// </param>
    /// <returns>The descriptor for method chaining.</returns>
    IAzureServiceBusQueueDescriptor Temporary(TimeSpan idleTimeout);

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

    /// <summary>
    /// Declares an explicit topic from which this queue receives messages, with an explicit
    /// provisioning opt-in or opt-out for the derived subscription, overriding the queue's setting.
    /// </summary>
    /// <param name="source">The source topic address.</param>
    /// <param name="autoProvision">Whether the subscription is provisioned on the broker.</param>
    /// <returns>The queue descriptor for chaining.</returns>
    IAzureServiceBusQueueDescriptor BindFrom(Uri source, bool autoProvision);
}
