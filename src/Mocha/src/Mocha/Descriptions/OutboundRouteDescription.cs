namespace Mocha;

/// <summary>
/// Describes an outbound route for diagnostic and visualization purposes.
/// </summary>
/// <param name="Kind">The kind of outbound route (publish or send).</param>
/// <param name="MessageTypeIdentity">The identity string of the message type being routed.</param>
/// <param name="Destination">The destination address URI string, or <c>null</c> if not yet resolved.</param>
/// <param name="Endpoint">The dispatch endpoint reference, or <c>null</c> if not yet assigned.</param>
public sealed record OutboundRouteDescription(
    OutboundRouteKind Kind,
    string MessageTypeIdentity,
    string? Destination,
    EndpointReferenceDescription? Endpoint);
