namespace Mocha;

/// <summary>
/// Maps route and endpoint kinds to the transport capabilities they require.
/// </summary>
internal static class MessagingTransportCapabilityMap
{
    public static MessagingTransportCapabilities GetRequiredCapability(InboundRouteKind kind)
        => kind switch
        {
            InboundRouteKind.Subscribe => MessagingTransportCapabilities.PublishSubscribe,
            InboundRouteKind.Send => MessagingTransportCapabilities.Send,
            InboundRouteKind.Request => MessagingTransportCapabilities.RequestReply,
            InboundRouteKind.Reply => MessagingTransportCapabilities.RequestReply,
            _ => MessagingTransportCapabilities.None
        };

    public static MessagingTransportCapabilities GetRequiredCapability(OutboundRouteKind kind)
        => kind switch
        {
            OutboundRouteKind.Send => MessagingTransportCapabilities.Send,
            OutboundRouteKind.Publish => MessagingTransportCapabilities.PublishSubscribe,
            _ => MessagingTransportCapabilities.None
        };

    public static MessagingTransportCapabilities GetRequiredCapability(ReceiveEndpointKind kind)
        => kind switch
        {
            ReceiveEndpointKind.Reply => MessagingTransportCapabilities.RequestReply,
            _ => MessagingTransportCapabilities.None
        };

    public static MessagingTransportCapabilities GetRequiredCapability(DispatchEndpointKind kind)
        => kind switch
        {
            DispatchEndpointKind.Reply => MessagingTransportCapabilities.RequestReply,
            _ => MessagingTransportCapabilities.None
        };
}
