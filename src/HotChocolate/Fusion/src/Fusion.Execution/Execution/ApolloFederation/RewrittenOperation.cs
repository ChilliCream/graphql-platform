using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.ApolloFederation;

/// <summary>
/// Represents a Fusion planner lookup query that has been rewritten into an
/// Apollo Federation <c>_entities</c> query.
/// </summary>
/// <param name="Operation">
/// The rewritten <c>_entities(representations: $representations)</c> operation
/// with all key and <c>@require</c> arguments stripped.
/// </param>
/// <param name="EntityTypeName">
/// The entity type name used for the <c>... on &lt;EntityType&gt;</c> condition
/// and the representation <c>__typename</c> (e.g. <c>"Product"</c>).
/// </param>
/// <param name="LookupField">
/// The original, un-stripped root lookup <see cref="FieldNode"/>. It still carries
/// the key argument bindings and inner <c>@require</c> variable arguments that the
/// execution pipeline needs to build representations.
/// </param>
internal readonly record struct RewrittenOperation(
    OperationSourceText Operation,
    string EntityTypeName,
    FieldNode LookupField);
