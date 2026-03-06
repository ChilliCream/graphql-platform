namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Fluent interface for configuring a RabbitMQ binding.
/// </summary>
public interface IRabbitMQBindingDescriptor : IMessagingDescriptor<RabbitMQBindingConfiguration>
{
    /// <summary>
    /// Sets the destination queue or exchange name.
    /// </summary>
    /// <param name="queueName">The name of the queue where messages will be routed.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQBindingDescriptor ToQueue(string queueName);

    /// <summary>
    /// Sets the destination exchange name.
    /// </summary>
    /// <param name="exchangeName">The name of the exchange where messages will be routed.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQBindingDescriptor ToExchange(string exchangeName);

    /// <summary>
    /// Sets the routing key for message routing.
    /// </summary>
    /// <param name="routingKey">The routing key pattern used for matching messages.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQBindingDescriptor RoutingKey(string routingKey);

    /// <summary>
    /// Adds a custom argument to the binding configuration.
    /// </summary>
    /// <param name="key">The argument key (used for headers exchange routing).</param>
    /// <param name="value">The argument value.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQBindingDescriptor WithArgument(string key, object value);

    /// <summary>
    /// Sets whether the binding should be automatically provisioned.
    /// </summary>
    /// <param name="autoProvision">True to enable auto-provisioning (default: true).</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQBindingDescriptor AutoProvision(bool autoProvision = true);
}
