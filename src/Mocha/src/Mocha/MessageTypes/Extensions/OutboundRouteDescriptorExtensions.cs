namespace Mocha;

/// <summary>
/// Provides convenience extension methods for configuring outbound route destinations using
/// transport-specific URI schemes.
/// </summary>
public static class OutboundRouteDescriptorExtensions
{
    /// <summary>
    /// Sets the outbound route destination to a queue with the specified name.
    /// </summary>
    /// <param name="descriptor">The outbound route descriptor.</param>
    /// <param name="queueName">The queue name.</param>
    /// <returns>The descriptor for method chaining.</returns>
    /// <remarks>
    /// The scheme-qualified destination uses a transport-neutral <c>queue:</c> scheme. In a multi-transport application,
    /// the target transport must be designated as the default transport (via <see cref="IMessagingTransportDescriptor.IsDefaultTransport"/>)
    /// or explicitly selected via <see cref="IOutboundRouteDescriptor.OnTransport"/>, otherwise the destination cannot be resolved unambiguously
    /// and an error will be raised at first dispatch.
    /// </remarks>
    public static IOutboundRouteDescriptor ToQueue(this IOutboundRouteDescriptor descriptor, string queueName)
        => descriptor.Destination(new Uri($"queue:{queueName}"));

    /// <summary>
    /// Sets the outbound route destination to an exchange with the specified name.
    /// </summary>
    /// <param name="descriptor">The outbound route descriptor.</param>
    /// <param name="exchangeName">The exchange name.</param>
    /// <returns>The descriptor for method chaining.</returns>
    /// <remarks>
    /// The scheme-qualified destination uses a transport-neutral <c>exchange:</c> scheme. In a multi-transport application,
    /// the target transport must be designated as the default transport (via <see cref="IMessagingTransportDescriptor.IsDefaultTransport"/>)
    /// or explicitly selected via <see cref="IOutboundRouteDescriptor.OnTransport"/>, otherwise the destination cannot be resolved unambiguously
    /// and an error will be raised at first dispatch.
    /// </remarks>
    public static IOutboundRouteDescriptor ToExchange(this IOutboundRouteDescriptor descriptor, string exchangeName)
        => descriptor.Destination(new Uri($"exchange:{exchangeName}"));

    /// <summary>
    /// Sets the outbound route destination to a topic with the specified name.
    /// </summary>
    /// <param name="descriptor">The outbound route descriptor.</param>
    /// <param name="topicName">The topic name.</param>
    /// <returns>The descriptor for method chaining.</returns>
    /// <remarks>
    /// The scheme-qualified destination uses a transport-neutral <c>topic:</c> scheme. In a multi-transport application,
    /// the target transport must be designated as the default transport (via <see cref="IMessagingTransportDescriptor.IsDefaultTransport"/>)
    /// or explicitly selected via <see cref="IOutboundRouteDescriptor.OnTransport"/>, otherwise the destination cannot be resolved unambiguously
    /// and an error will be raised at first dispatch.
    /// </remarks>
    public static IOutboundRouteDescriptor ToTopic(this IOutboundRouteDescriptor descriptor, string topicName)
        => descriptor.Destination(new Uri($"topic:{topicName}"));
}
