namespace Mocha;

/// <summary>
/// Provides a fluent API for configuring an inbound message route, including message type, response type, and route kind.
/// </summary>
public interface IInboundRouteDescriptor : IMessagingDescriptor<InboundRouteConfiguration>
{
    /// <summary>
    /// Sets the CLR type of the message that this route handles.
    /// </summary>
    /// <param name="messageType">The message CLR type.</param>
    /// <returns>This descriptor for method chaining.</returns>
    IInboundRouteDescriptor MessageType(Type messageType);

    /// <summary>
    /// Sets the CLR type of the response message for request-reply patterns.
    /// </summary>
    /// <param name="responseType">The response CLR type.</param>
    /// <returns>This descriptor for method chaining.</returns>
    IInboundRouteDescriptor ResponseType(Type responseType);

    /// <summary>
    /// Sets the kind of inbound route (subscribe, send, request, or reply).
    /// </summary>
    /// <param name="kind">The inbound route kind.</param>
    /// <returns>This descriptor for method chaining.</returns>
    IInboundRouteDescriptor Kind(InboundRouteKind kind);
}
