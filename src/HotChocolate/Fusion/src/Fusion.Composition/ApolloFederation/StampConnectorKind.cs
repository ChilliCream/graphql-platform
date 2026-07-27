using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.ApolloFederation;

/// <summary>
/// Records the Apollo Federation connector kind on the source schema so the composer can
/// surface it in <c>@fusion__schema_metadata(kind:)</c> in the merged execution schema.
/// </summary>
internal static class StampConnectorKind
{
    private const string ApolloKind = "ApolloFederation";

    /// <summary>
    /// Stamps the connector kind onto the schema's feature collection. Idempotent: a
    /// subsequent call replaces the previously recorded kind.
    /// </summary>
    /// <param name="schema">
    /// The mutable schema definition to stamp in place.
    /// </param>
    public static void Apply(MutableSchemaDefinition schema)
    {
        schema.Features.Set(new ConnectorKindMetadata(ApolloKind));
    }
}
