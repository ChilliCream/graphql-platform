namespace Mocha;

/// <summary>
/// Describes a single topology entity (e.g., queue, exchange, topic) within a transport.
/// </summary>
/// <param name="Kind">The kind of entity (e.g., "queue", "exchange", "topic").</param>
/// <param name="Name">The name of the entity, or <c>null</c> if unnamed.</param>
/// <param name="Address">The address of the entity, or <c>null</c> if not applicable.</param>
/// <param name="Flow">The flow direction of the entity, or <c>null</c> if not applicable.</param>
/// <param name="Properties">Additional transport-specific properties, or <c>null</c> if none.</param>
public sealed record TopologyEntityDescription(
    string Kind,
    string? Name,
    string? Address,
    string? Flow,
    IReadOnlyDictionary<string, object?>? Properties);
