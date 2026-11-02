namespace Mocha;

/// <summary>
/// Contains the inbound and outbound route descriptions for diagnostic output.
/// </summary>
/// <param name="Inbound">The inbound route descriptions.</param>
/// <param name="Outbound">The outbound route descriptions.</param>
public sealed record RoutesDescription(
    IReadOnlyList<InboundRouteDescription> Inbound,
    IReadOnlyList<OutboundRouteDescription> Outbound);
