using System.Collections.Immutable;
using HotChocolate.Fusion.Execution.ApolloFederation;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

/// <summary>
/// Holds the rewritten pieces of a single Apollo Federation entity lookup. A batch
/// node holds one lookup per operation definition, index-aligned with them.
/// </summary>
/// <param name="Operation">
/// The rewritten single-lookup <c>_entities</c> operation that is sent for
/// this lookup.
/// </param>
/// <param name="OperationDocument">
/// The parsed syntax tree of the rewritten operation source text.
/// </param>
/// <param name="EntityTypeName">
/// The entity type name used for the <c>... on EntityType</c> condition and the
/// representation <c>__typename</c>.
/// </param>
/// <param name="RepresentationShape">
/// The representation shape compiled from the operation and the requirements.
/// A default value is construction-only. A lookup stored by an execution node
/// always contains a materialized, non-default shape, which may be empty.
/// </param>
internal readonly record struct ApolloEntityLookup(
    OperationSourceText Operation,
    Utf8OperationDocument OperationDocument,
    string EntityTypeName,
    ImmutableArray<RepresentationShapeNode> RepresentationShape);
