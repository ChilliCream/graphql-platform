using Azure.Messaging.ServiceBus;

namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Fluent interface for configuring an Azure Service Bus receive endpoint, including its backing queue,
/// handlers, prefetch count, and receive middleware pipeline.
/// </summary>
public interface IAzureServiceBusReceiveEndpointDescriptor
    : IReceiveEndpointDescriptor<AzureServiceBusReceiveEndpointConfiguration>
{
    /// <inheritdoc cref="IReceiveEndpointDescriptor{T}.Handler{THandler}"/>
    new IAzureServiceBusReceiveEndpointDescriptor Handler<THandler>() where THandler : class, IHandler;

    /// <inheritdoc cref="IReceiveEndpointDescriptor{T}.Consumer{TConsumer}"/>
    new IAzureServiceBusReceiveEndpointDescriptor Consumer<TConsumer>() where TConsumer : class, IConsumer;

    /// <inheritdoc cref="IReceiveEndpointDescriptor{T}.Kind(ReceiveEndpointKind)"/>
    new IAzureServiceBusReceiveEndpointDescriptor Kind(ReceiveEndpointKind kind);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{T}.FaultEndpoint(string)"/>
    new IAzureServiceBusReceiveEndpointDescriptor FaultEndpoint(string name);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{T}.SkippedEndpoint(string)"/>
    new IAzureServiceBusReceiveEndpointDescriptor SkippedEndpoint(string name);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{T}.MaxConcurrency(int)"/>
    /// <remarks>
    /// On a session-bound endpoint, when <see cref="WithMaxConcurrentSessions"/> is not explicitly
    /// set, this value is reinterpreted as the maximum number of concurrently locked sessions
    /// (see <see cref="ServiceBusSessionProcessorOptions.MaxConcurrentSessions"/>). The resolved
    /// translation is logged at endpoint startup.
    /// </remarks>
    new IAzureServiceBusReceiveEndpointDescriptor MaxConcurrency(int maxConcurrency);

    /// <summary>
    /// Sets the name of the Azure Service Bus queue this endpoint will consume from.
    /// </summary>
    /// <param name="name">The queue name.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IAzureServiceBusReceiveEndpointDescriptor Queue(string name);

    /// <summary>
    /// Sets the prefetch count for this endpoint, controlling how many messages are prefetched from the broker.
    /// A value of zero disables prefetching; pass <see langword="null"/> to fall back to the computed default.
    /// </summary>
    /// <param name="count">The prefetch count, or <see langword="null"/> to use the computed default.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IAzureServiceBusReceiveEndpointDescriptor PrefetchCount(int? count);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{T}.UseReceive(ReceiveMiddlewareConfiguration, string?, string?)"/>
    new IAzureServiceBusReceiveEndpointDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null);

    /// <summary>
    /// Opts the endpoint's underlying queue into forwarding broker-dead-lettered messages
    /// (<c>MaxDeliveryCountExceeded</c>, <c>TTLExpiredException</c>) into the Mocha-managed
    /// <c>{queueName}_error</c> queue, consolidating fault visibility.
    /// </summary>
    /// <remarks>
    /// Conflicts with an explicitly configured <c>ForwardDeadLetteredMessagesTo</c> on the same queue.
    /// </remarks>
    /// <returns>The descriptor for method chaining.</returns>
    IAzureServiceBusReceiveEndpointDescriptor UseNativeDeadLetterForwarding();

    /// <summary>
    /// Sets the maximum number of concurrently locked sessions on a session-bound endpoint
    /// (mapped to <see cref="ServiceBusSessionProcessorOptions.MaxConcurrentSessions"/>).
    /// Has no effect on non-session endpoints; setting it on a non-session queue causes
    /// endpoint startup to fail loudly.
    /// </summary>
    /// <param name="maxConcurrentSessions">The maximum number of concurrent sessions.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IAzureServiceBusReceiveEndpointDescriptor WithMaxConcurrentSessions(int maxConcurrentSessions);

    /// <summary>
    /// Sets the maximum number of concurrent message dispatches per locked session
    /// (mapped to <see cref="ServiceBusSessionProcessorOptions.MaxConcurrentCallsPerSession"/>).
    /// Defaults to <c>1</c> on session endpoints to preserve in-session ordering. Has no effect
    /// on non-session endpoints; setting it on a non-session queue causes endpoint startup to
    /// fail loudly.
    /// </summary>
    /// <param name="maxConcurrentCallsPerSession">The maximum number of concurrent calls per session.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IAzureServiceBusReceiveEndpointDescriptor WithMaxConcurrentCallsPerSession(int maxConcurrentCallsPerSession);

    /// <summary>
    /// Sets the duration the session processor will wait for new messages on a locked session
    /// before releasing the session lock
    /// (mapped to <see cref="ServiceBusSessionProcessorOptions.SessionIdleTimeout"/>). Has no
    /// effect on non-session endpoints; setting it on a non-session queue causes endpoint startup
    /// to fail loudly.
    /// </summary>
    /// <param name="sessionIdleTimeout">The session idle timeout.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IAzureServiceBusReceiveEndpointDescriptor WithSessionIdleTimeout(TimeSpan sessionIdleTimeout);

    /// <summary>
    /// Sets the maximum total duration over which the SDK will auto-renew the message lock.
    /// On session endpoints the same duration also bounds session-lock renewal (the SDK uses one
    /// duration for both lock kinds). Defaults to five minutes on both modes.
    /// </summary>
    /// <param name="maxAutoLockRenewalDuration">The maximum auto lock renewal duration.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IAzureServiceBusReceiveEndpointDescriptor WithMaxAutoLockRenewalDuration(TimeSpan maxAutoLockRenewalDuration);
}
