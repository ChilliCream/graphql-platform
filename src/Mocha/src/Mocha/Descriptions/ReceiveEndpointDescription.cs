namespace Mocha;

/// <summary>
/// Describes a receive endpoint for diagnostic and visualization purposes.
/// </summary>
/// <param name="Name">The logical name of the receive endpoint.</param>
/// <param name="Kind">The kind of receive endpoint (e.g., queue, subscription).</param>
/// <param name="Address">The transport-level address URI, or <c>null</c> if not yet assigned.</param>
/// <param name="SourceAddress">The source address for subscription-type endpoints, or <c>null</c> if not applicable.</param>
public sealed record ReceiveEndpointDescription(
    string Name,
    ReceiveEndpointKind Kind,
    string? Address,
    string? SourceAddress);
