namespace HotChocolate.Fusion.Execution.Clients;

/// <summary>
/// Describes a single lookup field on the composite schema that maps
/// to an Apollo Federation entity type. Used by <see cref="FederationQueryRewriter"/>
/// to detect entity lookups and rewrite them into <c>_entities</c> queries.
/// </summary>
internal sealed class LookupFieldInfo
{
    /// <summary>
    /// Gets the entity type name this lookup resolves (e.g. <c>"Product"</c>).
    /// </summary>
    public required string EntityTypeName { get; init; }

    /// <summary>
    /// Maps argument name (e.g. <c>"id"</c>) to entity key field name (e.g. <c>"id"</c>).
    /// </summary>
    public required IReadOnlyDictionary<string, string> ArgumentToKeyFieldMap { get; init; }
}
