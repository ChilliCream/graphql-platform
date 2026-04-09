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
    /// </summary>
    /// <param name="count">The prefetch count.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IAzureServiceBusReceiveEndpointDescriptor PrefetchCount(int count);

    /// <inheritdoc cref="IReceiveEndpointDescriptor{T}.UseReceive(ReceiveMiddlewareConfiguration, string?, string?)"/>
    new IAzureServiceBusReceiveEndpointDescriptor UseReceive(
        ReceiveMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null);
}
