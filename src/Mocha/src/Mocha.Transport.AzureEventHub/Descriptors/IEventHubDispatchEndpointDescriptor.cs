namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Fluent interface for configuring an Event Hub dispatch endpoint, including hub targeting and middleware pipeline.
/// </summary>
public interface IEventHubDispatchEndpointDescriptor
    : IDispatchEndpointDescriptor<EventHubDispatchEndpointConfiguration>
{
    /// <summary>
    /// Sets the target Event Hub name for outbound message dispatch.
    /// </summary>
    /// <param name="name">The Event Hub entity name.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IEventHubDispatchEndpointDescriptor ToHub(string name);

    /// <summary>
    /// Sets the static partition ID for outbound message dispatch.
    /// When set, all messages from this endpoint are sent to the specified partition.
    /// </summary>
    /// <param name="partitionId">The target partition ID.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IEventHubDispatchEndpointDescriptor PartitionId(string partitionId);

    /// <summary>
    /// Sets the batch mode for this dispatch endpoint.
    /// </summary>
    /// <param name="mode">The batch mode to use.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IEventHubDispatchEndpointDescriptor BatchMode(EventHubBatchMode mode);

    /// <inheritdoc cref="IDispatchEndpointDescriptor{TConfiguration}.Send{TMessage}" />
    new IEventHubDispatchEndpointDescriptor Send<TMessage>();

    /// <inheritdoc cref="IDispatchEndpointDescriptor{TConfiguration}.Publish{TMessage}" />
    new IEventHubDispatchEndpointDescriptor Publish<TMessage>();

    /// <inheritdoc cref="IDispatchEndpointDescriptor{TConfiguration}.UseDispatch" />
    new IEventHubDispatchEndpointDescriptor UseDispatch(
        DispatchMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null);
}
