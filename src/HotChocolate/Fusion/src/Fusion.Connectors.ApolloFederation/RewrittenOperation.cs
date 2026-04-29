using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// Represents a Fusion planner query that has been rewritten for an
/// Apollo Federation subgraph. The rewritten text may contain an
/// <c>_entities</c> query (for entity lookups) or the original query
/// text unchanged (for passthrough fields).
/// </summary>
internal sealed class RewrittenOperation
{
    /// <summary>
    /// Gets the rewritten GraphQL query string. For entity lookups this
    /// contains the <c>_entities(representations: $representations)</c> query;
    /// for passthrough queries this is the original operation text.
    /// </summary>
    public required string OperationText { get; init; }

    /// <summary>
    /// Gets whether this operation is an entity lookup (<c>true</c>) or
    /// a passthrough query (<c>false</c>).
    /// </summary>
    public required bool IsEntityLookup { get; init; }

    /// <summary>
    /// Gets the entity type name for the <c>__typename</c> value in
    /// representations (e.g. <c>"Product"</c>). <c>null</c> for passthrough queries.
    /// </summary>
    public required string? EntityTypeName { get; init; }

    /// <summary>
    /// Maps variable names from the planner query (e.g. <c>"__fusion_1_id"</c>)
    /// to entity key field names (e.g. <c>"id"</c>). Empty for passthrough queries.
    /// </summary>
    public required IReadOnlyDictionary<string, string> VariableToKeyFieldMap { get; init; }

    /// <summary>
    /// Gets the name of the lookup field in the original planner query
    /// (e.g. <c>"productById"</c>). Used to wrap individual entity results
    /// back into the shape the Fusion execution pipeline expects.
    /// <c>null</c> for passthrough queries.
    /// </summary>
    public required string? LookupFieldName { get; init; }

    /// <summary>
    /// Gets the inline fragment for this entity type
    /// (e.g. <c>... on Product { id name }</c>). Used when building batched
    /// aliased queries. <c>null</c> for passthrough queries.
    /// </summary>
    public InlineFragmentNode? InlineFragment { get; init; }
}
