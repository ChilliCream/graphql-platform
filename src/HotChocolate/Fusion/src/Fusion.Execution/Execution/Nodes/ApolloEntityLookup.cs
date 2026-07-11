using HotChocolate.Fusion.Execution.ApolloFederation;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Holds the rewritten per-lookup pieces of an Apollo Federation entity batch,
/// index-aligned with the batch node's operation definitions.
/// </summary>
/// <param name="Operation">
/// The rewritten single-lookup <c>_entities</c> operation that is sent for
/// this lookup.
/// </param>
/// <param name="OperationHash">
/// The xxhash64 of the rewritten operation source text.
/// </param>
/// <param name="EntityTypeName">
/// The entity type name used for the <c>... on EntityType</c> condition and the
/// representation <c>__typename</c>.
/// </param>
/// <param name="RepresentationShape">
/// The representation shape compiled from the lookup field and the requirements.
/// It is a plan-time constant and is reused for every request this lookup serves.
/// </param>
internal readonly record struct ApolloEntityLookup(
    OperationSourceText Operation,
    ulong OperationHash,
    string EntityTypeName,
    List<RepresentationShapeNode> RepresentationShape);
