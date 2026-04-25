namespace Mocha;

/// <summary>
/// Describes an inbound route binding a message type to a consumer and endpoint.
/// </summary>
/// <param name="Kind">The kind of inbound route (subscribe, send, or request).</param>
/// <param name="MessageTypeIdentity">The identity string of the message type, or <c>null</c> if unknown.</param>
/// <param name="ConsumerName">The name of the consumer handling messages on this route, or <c>null</c> if unbound.</param>
/// <param name="Endpoint">The endpoint reference, or <c>null</c> if not yet assigned.</param>
internal sealed record InboundRouteDescription(
    InboundRouteKind Kind,
    string? MessageTypeIdentity,
    string? ConsumerName,
    EndpointReferenceDescription? Endpoint);
