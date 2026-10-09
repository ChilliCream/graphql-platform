using HotChocolate.Fusion.Extensions;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.ApolloFederation;

/// <summary>
/// Generates <c>@lookup</c> metadata for Apollo Federation node fields.
/// </summary>
internal static class GenerateNodeLookup
{
    /// <summary>
    /// Marks a subgraph's <c>node(id:): Node</c> field as <c>@lookup</c>
    /// so it composes as a node lookup, mirroring native composition.
    /// </summary>
    /// <param name="schema">
    /// The mutable schema definition to transform in place.
    /// </param>
    public static void Apply(MutableSchemaDefinition schema)
    {
        if (schema.QueryType is null)
        {
            return;
        }

        if (!schema.Types.TryGetType<IInterfaceTypeDefinition>(
            WellKnownTypeNames.Node,
            out var nodeType))
        {
            return;
        }

        foreach (var field in schema.QueryType.Fields)
        {
            if (field.Name == WellKnownFieldNames.Node
                && field.Arguments.Count == 1
                && !field.Type.IsListType()
                && field.Type.Kind != TypeKind.NonNull
                && !field.Directives.ContainsName(WellKnownDirectiveNames.Lookup)
                && ReferenceEquals(field.Type.NamedType(), nodeType))
            {
                field.ApplyLookupDirective();
            }
        }
    }
}
