namespace Mocha;

/// <summary>
/// Provides a fluent API for configuring a message type, including serializers and outbound routes.
/// </summary>
public interface IMessageTypeDescriptor : IMessagingDescriptor<MessageTypeConfiguration>
{
    /// <summary>
    /// Registers a custom serializer for this message type.
    /// </summary>
    /// <param name="messageSerializer">The serializer to register.</param>
    /// <returns>This descriptor for method chaining.</returns>
    IMessageTypeDescriptor AddSerializer(IMessageSerializer messageSerializer);

    /// <summary>
    /// Configures a publish (fan-out) outbound route for this message type.
    /// </summary>
    /// <param name="configure">The action to configure the outbound route.</param>
    /// <returns>This descriptor for method chaining.</returns>
    IMessageTypeDescriptor Publish(Action<IOutboundRouteDescriptor> configure);

    /// <summary>
    /// Configures a send (point-to-point) outbound route for this message type.
    /// </summary>
    /// <param name="configure">The action to configure the outbound route.</param>
    /// <returns>This descriptor for method chaining.</returns>
    IMessageTypeDescriptor Send(Action<IOutboundRouteDescriptor> configure);
}
