namespace Mocha.Transport.InMemory;

/// <summary>
/// Fluent interface for configuring an in-memory messaging transport, including endpoints,
/// topology resources, middleware pipelines, conventions, and handler binding strategies.
/// </summary>
public interface IInMemoryMessagingTransportDescriptor : IMessagingTransportDescriptor
{
    /// <inheritdoc />
    new IInMemoryMessagingTransportDescriptor ModifyOptions(Action<TransportOptions> configure);

    /// <inheritdoc />
    new IInMemoryMessagingTransportDescriptor Schema(string schema);

    /// <inheritdoc />
    new IInMemoryMessagingTransportDescriptor BindHandlersImplicitly();

    /// <inheritdoc />
    new IInMemoryMessagingTransportDescriptor BindHandlersExplicitly();

    /// <summary>
    /// Declares or retrieves a receive endpoint with the specified name.
    /// </summary>
    /// <param name="name">The endpoint name, which also defaults as the queue name.</param>
    /// <returns>A descriptor for further configuring the receive endpoint.</returns>
    IInMemoryReceiveEndpointDescriptor Endpoint(string name);

    /// <summary>
    /// Declares or retrieves a dispatch endpoint with the specified name.
    /// </summary>
    /// <param name="name">The endpoint name, which also defaults as the topic name.</param>
    /// <returns>A descriptor for further configuring the dispatch endpoint.</returns>
    IInMemoryDispatchEndpointDescriptor DispatchEndpoint(string name);

    /// <summary>
    /// Declares a topic in the in-memory topology.
    /// </summary>
    /// <param name="name">The topic name.</param>
    /// <returns>A descriptor for further configuring the topic.</returns>
    IInMemoryTopicDescriptor DeclareTopic(string name);

    /// <summary>
    /// Declares a queue in the in-memory topology.
    /// </summary>
    /// <param name="name">The queue name.</param>
    /// <returns>A descriptor for further configuring the queue.</returns>
    IInMemoryQueueDescriptor DeclareQueue(string name);

    /// <summary>
    /// Declares a binding that routes messages from a topic to a queue in the in-memory topology.
    /// </summary>
    /// <param name="topic">The source topic name.</param>
    /// <param name="queue">The destination queue name.</param>
    /// <returns>A descriptor for further configuring the binding.</returns>
    IInMemoryBindingDescriptor DeclareBinding(string topic, string queue);

    /// <inheritdoc />
    new IInMemoryMessagingTransportDescriptor Name(string name);

    /// <inheritdoc />
    new IInMemoryMessagingTransportDescriptor AddConvention(IConvention convention);

    /// <inheritdoc />
    new IInMemoryMessagingTransportDescriptor IsDefaultTransport();

    /// <inheritdoc />
    new IInMemoryMessagingTransportDescriptor UseDispatch(DispatchMiddlewareConfiguration configuration);

    /// <inheritdoc />
    new IInMemoryMessagingTransportDescriptor AppendDispatch(
        string after,
        DispatchMiddlewareConfiguration configuration);

    /// <inheritdoc />
    new IInMemoryMessagingTransportDescriptor PrependDispatch(
        string before,
        DispatchMiddlewareConfiguration configuration);

    /// <inheritdoc />
    new IInMemoryMessagingTransportDescriptor UseReceive(ReceiveMiddlewareConfiguration configuration);

    /// <inheritdoc />
    new IInMemoryMessagingTransportDescriptor AppendReceive(string after, ReceiveMiddlewareConfiguration configuration);

    /// <inheritdoc />
    new IInMemoryMessagingTransportDescriptor PrependReceive(
        string before,
        ReceiveMiddlewareConfiguration configuration);
}
