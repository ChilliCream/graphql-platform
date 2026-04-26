namespace Mocha;

/// <summary>
/// Describes a dispatch endpoint for diagnostic and visualization purposes.
/// </summary>
/// <param name="Name">The logical name of the dispatch endpoint.</param>
/// <param name="Kind">The kind of dispatch endpoint (e.g., direct, topic).</param>
/// <param name="Address">The transport-level address URI, or <c>null</c> if not yet assigned.</param>
/// <param name="DestinationAddress">The resolved destination address, or <c>null</c> if not applicable.</param>
public sealed record DispatchEndpointDescription(
    string Name,
    DispatchEndpointKind Kind,
    string? Address,
    string? DestinationAddress);
