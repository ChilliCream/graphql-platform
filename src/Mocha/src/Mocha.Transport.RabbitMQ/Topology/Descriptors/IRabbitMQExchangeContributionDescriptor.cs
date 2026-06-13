namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Fluent interface for contributing partial exchange property declarations to a message type.
/// Contributed properties are merged onto the convention exchange using the 3.5 merge rules:
/// declared non-null scalar wins, convention fills the rest, Arguments union per key.
/// </summary>
public interface IRabbitMQExchangeContributionDescriptor
{
    /// <summary>
    /// Sets the type of the exchange.
    /// </summary>
    /// <param name="type">The exchange type (e.g., "direct", "fanout", "topic", "headers").</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQExchangeContributionDescriptor Type(string type);

    /// <summary>
    /// Sets whether the exchange survives broker restarts.
    /// </summary>
    /// <param name="durable">True to make the exchange durable (default: true).</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQExchangeContributionDescriptor Durable(bool durable = true);

    /// <summary>
    /// Sets whether the exchange is automatically deleted when no longer in use.
    /// </summary>
    /// <param name="autoDelete">True to enable auto-deletion (default: true).</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQExchangeContributionDescriptor AutoDelete(bool autoDelete = true);

    /// <summary>
    /// Adds a custom argument to the exchange configuration.
    /// </summary>
    /// <param name="key">The argument key.</param>
    /// <param name="value">The argument value.</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQExchangeContributionDescriptor WithArgument(string key, object value);

    /// <summary>
    /// Sets whether the exchange should be automatically provisioned.
    /// </summary>
    /// <param name="autoProvision">True to enable auto-provisioning (default: true).</param>
    /// <returns>The descriptor for method chaining.</returns>
    IRabbitMQExchangeContributionDescriptor AutoProvision(bool autoProvision = true);
}
