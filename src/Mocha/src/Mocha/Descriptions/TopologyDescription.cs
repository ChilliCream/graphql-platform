namespace Mocha;

/// <summary>
/// Describes the transport-level topology (entities and links) for a transport.
/// </summary>
/// <param name="Address">The base address of the topology, or <c>null</c> if not applicable.</param>
/// <param name="Entities">The topology entities (queues, exchanges, topics).</param>
/// <param name="Links">The topology links (bindings, subscriptions) between entities.</param>
public sealed record TopologyDescription(
    string? Address,
    IReadOnlyList<TopologyEntityDescription> Entities,
    IReadOnlyList<TopologyLinkDescription> Links);
