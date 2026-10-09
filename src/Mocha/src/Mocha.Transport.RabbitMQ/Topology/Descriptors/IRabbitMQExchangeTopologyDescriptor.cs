namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Fluent interface for configuring a RabbitMQ exchange.
/// </summary>
public interface IRabbitMQExchangeTopologyDescriptor : IMessagingDescriptor<RabbitMQExchangeConfiguration>
{
    /// <summary>
    /// Sets the name of the exchange.
    /// </summary>
    /// <param name="name">The exchange name.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQExchangeTopologyDescriptor Name(string name);

    /// <summary>
    /// Sets the type of the exchange.
    /// </summary>
    /// <param name="type">The exchange type (Direct, Fanout, Topic, or Headers).</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQExchangeTopologyDescriptor Type(string type);

    /// <summary>
    /// Sets whether the exchange survives broker restarts.
    /// </summary>
    /// <param name="durable">True to make the exchange durable (default: true).</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQExchangeTopologyDescriptor Durable(bool durable = true);

    /// <summary>
    /// Sets whether the exchange is automatically deleted when no longer in use.
    /// </summary>
    /// <param name="autoDelete">True to enable auto-deletion (default: true).</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQExchangeTopologyDescriptor AutoDelete(bool autoDelete = true);

    /// <summary>
    /// Adds a custom argument to the exchange configuration.
    /// </summary>
    /// <param name="key">The argument key (e.g., "alternate-exchange").</param>
    /// <param name="value">The argument value.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQExchangeTopologyDescriptor WithArgument(string key, object value);

    /// <summary>
    /// Sets whether the exchange should be automatically provisioned.
    /// </summary>
    /// <param name="autoProvision">True to enable auto-provisioning (default: true).</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQExchangeTopologyDescriptor AutoProvision(bool autoProvision = true);
}
