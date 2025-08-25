using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types;

/// <summary>
/// Provides lookup functionality on where a `Node` can be resolved from when using the `Query.node` field.
/// This lookup is only uses for fallback resolution. The `Node` could actually be resolvable from more than
/// one schema.
/// </summary>
internal sealed class NodeFallbackLookup : INeedsCompletion
{
    private FrozenDictionary<string, string> _schemaByType = FrozenDictionary<string, string>.Empty;

    /// <summary>
    /// Tries to determine a possible schema to do a node lookup for the provided <paramref name="typeName"/>.
    /// </summary>
    public bool TryGetNodeLookupSchemaForType(string typeName, [NotNullWhen(true)] out string? schemaName)
        => _schemaByType.TryGetValue(typeName, out schemaName);

    void INeedsCompletion.Complete(FusionSchemaDefinition schema, CompositeSchemaBuilderContext context)
    {
        if (!schema.Types.TryGetType<IInterfaceTypeDefinition>("Node", out var nodeType)
            || !schema.QueryType.Fields.TryGetField("node", out var nodeField)
            || nodeField.Type != nodeType)
        {
            return;
        }

        var lookup = new Dictionary<string, string>();
        foreach (var possibleType in schema.GetPossibleTypes(nodeType))
        {
            var nodeLookup = schema
                .GetPossibleLookups(possibleType)
                .FirstOrDefault(
                    l => l.Fields is [PathNode { PathSegment.FieldName.Value: "id" }]
                        && l.FieldName == "node");

            if (nodeLookup is null)
            {
                continue;
            }

            lookup.Add(possibleType.Name, nodeLookup.SchemaName);
        }

        _schemaByType = lookup.ToFrozenDictionary();
    }
}
