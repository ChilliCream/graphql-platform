namespace Mocha.Transport.NATS;

/// <summary>
/// Fluent interface for configuring a NATS dispatch endpoint, including subject targeting and middleware pipeline.
/// </summary>
public interface INatsDispatchEndpointDescriptor
    : IDispatchEndpointDescriptor<NatsDispatchEndpointConfiguration>
{
    /// <summary>
    /// Sets the endpoint to dispatch messages to the specified subject.
    /// </summary>
    /// <param name="name">The target subject name.</param>
    /// <returns>The descriptor for method chaining.</returns>
    INatsDispatchEndpointDescriptor ToSubject(string name);

    /// <inheritdoc cref="IDispatchEndpointDescriptor{TConfiguration}.Send{TMessage}" />
    new INatsDispatchEndpointDescriptor Send<TMessage>();

    /// <inheritdoc cref="IDispatchEndpointDescriptor{TConfiguration}.Publish{TMessage}" />
    new INatsDispatchEndpointDescriptor Publish<TMessage>();

    /// <inheritdoc cref="IDispatchEndpointDescriptor{TConfiguration}.UseDispatch" />
    new INatsDispatchEndpointDescriptor UseDispatch(DispatchMiddlewareConfiguration configuration);

    /// <inheritdoc cref="IDispatchEndpointDescriptor{TConfiguration}.AppendDispatch" />
    new INatsDispatchEndpointDescriptor AppendDispatch(string after, DispatchMiddlewareConfiguration configuration);

    /// <inheritdoc cref="IDispatchEndpointDescriptor{TConfiguration}.PrependDispatch" />
    new INatsDispatchEndpointDescriptor PrependDispatch(
        string before,
        DispatchMiddlewareConfiguration configuration);
}
