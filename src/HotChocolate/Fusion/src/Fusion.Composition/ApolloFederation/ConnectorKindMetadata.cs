namespace HotChocolate.Fusion.ApolloFederation;

/// <summary>
/// Processing-only metadata that records the connector kind of a source schema
/// (for example <c>"ApolloFederation"</c>) so the composer can surface it on
/// <c>@fusion__schema_metadata(kind:)</c> of the corresponding
/// <c>fusion__Schema</c> enum value in the merged schema. Carried on the source
/// schema's feature collection during composition and never serialized.
/// </summary>
internal sealed class ConnectorKindMetadata
{
    public ConnectorKindMetadata(string kind)
    {
        ArgumentException.ThrowIfNullOrEmpty(kind);
        Kind = kind;
    }

    public string Kind { get; }
}
