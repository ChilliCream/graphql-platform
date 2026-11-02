namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Fluent interface for configuring a RabbitMQ dispatch endpoint, including queue/exchange targeting and middleware pipeline.
/// </summary>
public interface IRabbitMQDispatchEndpointDescriptor
    : IDispatchEndpointDescriptor<RabbitMQDispatchEndpointConfiguration>
{
    /// <summary>
    /// Sets the endpoint to dispatch messages to the specified queue, clearing any exchange target.
    /// </summary>
    /// <param name="name">The target queue name.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQDispatchEndpointDescriptor ToQueue(string name);

    /// <summary>
    /// Sets the endpoint to dispatch messages to the specified exchange, clearing any queue target.
    /// </summary>
    /// <param name="name">The target exchange name.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQDispatchEndpointDescriptor ToExchange(string name);

    /// <inheritdoc cref="IDispatchEndpointDescriptor{TConfiguration}.Send{TMessage}" />
    new IRabbitMQDispatchEndpointDescriptor Send<TMessage>();

    /// <inheritdoc cref="IDispatchEndpointDescriptor{TConfiguration}.Publish{TMessage}" />
    new IRabbitMQDispatchEndpointDescriptor Publish<TMessage>();

    /// <inheritdoc cref="IDispatchEndpointDescriptor{TConfiguration}.UseDispatch" />
    new IRabbitMQDispatchEndpointDescriptor UseDispatch(
        DispatchMiddlewareConfiguration configuration,
        string? before = null,
        string? after = null);
}
