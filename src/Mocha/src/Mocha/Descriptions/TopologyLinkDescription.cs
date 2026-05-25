namespace Mocha;

/// <summary>
/// Describes a link (binding or subscription) between topology entities within a transport.
/// </summary>
/// <param name="Kind">The kind of link (e.g., "binding", "subscription").</param>
/// <param name="Address">The address of the link, or <c>null</c> if not applicable.</param>
/// <param name="Source">The source entity name, or <c>null</c> if not applicable.</param>
/// <param name="Target">The target entity name, or <c>null</c> if not applicable.</param>
/// <param name="Direction">The direction of the link, or <c>null</c> if not applicable.</param>
/// <param name="Properties">Additional transport-specific properties, or <c>null</c> if none.</param>
public sealed record TopologyLinkDescription(
    string Kind,
    string? Address,
    string? Source,
    string? Target,
    string? Direction,
    IReadOnlyDictionary<string, object?>? Properties);
