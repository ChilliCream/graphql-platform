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
}
