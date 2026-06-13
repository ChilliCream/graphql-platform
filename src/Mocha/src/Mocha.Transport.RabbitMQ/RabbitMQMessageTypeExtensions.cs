using Mocha.Features;

namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Extension methods for contributing partial exchange declarations to a message type
/// via the message type's feature collection.
/// </summary>
public static class RabbitMQMessageTypeExtensions
{
    /// <summary>
    /// Contributes a partial exchange declaration for the publish (fan-out) exchange of this message type.
    /// The declared properties are merged onto the convention exchange using the 3.5 merge rules:
    /// declared non-null scalar wins, convention fills the rest, Arguments union per key.
    /// </summary>
    /// <param name="descriptor">The message type descriptor.</param>
    /// <param name="configure">An action that configures the exchange properties to contribute.</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static IMessageTypeDescriptor PublishExchange(
        this IMessageTypeDescriptor descriptor,
        Action<IRabbitMQExchangeContributionDescriptor> configure)
    {
        var features = descriptor.Extend().Configuration.Features;
        var feature = features.GetOrSet<RabbitMQPublishExchangeFeature>();
        configure(feature);
        return descriptor;
    }

    /// <summary>
    /// Contributes a partial exchange declaration for the send (point-to-point) exchange of this message type.
    /// The declared properties are merged onto the convention exchange using the 3.5 merge rules:
    /// declared non-null scalar wins, convention fills the rest, Arguments union per key.
    /// </summary>
    /// <param name="descriptor">The message type descriptor.</param>
    /// <param name="configure">An action that configures the exchange properties to contribute.</param>
    /// <returns>The descriptor for method chaining.</returns>
    public static IMessageTypeDescriptor SendExchange(
        this IMessageTypeDescriptor descriptor,
        Action<IRabbitMQExchangeContributionDescriptor> configure)
    {
        var features = descriptor.Extend().Configuration.Features;
        var feature = features.GetOrSet<RabbitMQSendExchangeFeature>();
        configure(feature);
        return descriptor;
    }
}
