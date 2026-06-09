namespace Mocha;

/// <summary>
/// Describes an inbound route binding a message type to a consumer and endpoint.
/// </summary>
/// <param name="Kind">The kind of inbound route (subscribe, send, request, or reply).</param>
/// <param name="MessageTypeIdentity">The identity string of the message type, or <c>null</c> if unknown.</param>
/// <param name="ConsumerName">The name of the consumer handling messages on this route, or <c>null</c> if unbound.</param>
/// <param name="Condition">The condition that decides whether this route selects its consumer for a received message.</param>
/// <param name="Endpoint">The endpoint reference, or <c>null</c> if not yet assigned.</param>
public sealed record InboundRouteDescription(
    InboundRouteKind Kind,
    string? MessageTypeIdentity,
    string? ConsumerName,
    RouteConditionDescription Condition,
    EndpointReferenceDescription? Endpoint);
