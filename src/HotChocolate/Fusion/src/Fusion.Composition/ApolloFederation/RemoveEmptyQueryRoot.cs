using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.ApolloFederation;

/// <summary>
/// Removes an empty query root from a mutable source schema.
/// </summary>
internal static class RemoveEmptyQueryRoot
{
    public static void Apply(MutableSchemaDefinition schema)
    {
        if (schema.QueryType is not { Fields.Count: 0 } emptyQueryRoot)
        {
            return;
        }

        schema.Types.Remove(emptyQueryRoot);
        schema.QueryType = null;
    }
}
