namespace Mocha.Transport.AzureServiceBus;

/// <summary>
/// Fluent interface for configuring an Azure Service Bus dispatch endpoint, including its target
/// destination and dispatch middleware pipeline.
/// </summary>
public interface IAzureServiceBusDispatchEndpointDescriptor
    : IDispatchEndpointDescriptor<AzureServiceBusDispatchEndpointConfiguration>
{
    /// <summary>
    /// Directs this endpoint to dispatch messages to the specified Azure Service Bus queue.
    /// </summary>
    /// <param name="name">The name of the target queue.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IAzureServiceBusDispatchEndpointDescriptor ToQueue(string name);

    /// <summary>
    /// Directs this endpoint to dispatch messages to the specified Azure Service Bus topic.
    /// </summary>
    /// <param name="name">The name of the target topic.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IAzureServiceBusDispatchEndpointDescriptor ToTopic(string name);

    /// <inheritdoc cref="IDispatchEndpointDescriptor{T}.Send{TMessage}"/>
    new IAzureServiceBusDispatchEndpointDescriptor Send<TMessage>();

    /// <inheritdoc cref="IDispatchEndpointDescriptor{T}.Publish{TMessage}"/>
    new IAzureServiceBusDispatchEndpointDescriptor Publish<TMessage>();

    /// <inheritdoc cref="IDispatchEndpointDescriptor{T}.UseDispatch(DispatchMiddlewareConfiguration, string?, string?)"/>
    new IAzureServiceBusDispatchEndpointDescriptor UseDispatch(
        DispatchMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null);
}
