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
    private string[] _sourceSchemaNodeLookupSchemas = [];

    /// <summary>
    /// Tries to determine a possible schema to do a node lookup for the provided <paramref name="typeName"/>.
    /// </summary>
    public bool TryGetNodeLookupSchemaForType(string typeName, [NotNullWhen(true)] out string? schemaName)
        => _schemaByType.TryGetValue(typeName, out schemaName);

    /// <summary>
    /// Tries to determine any source schema that owns a node lookup, used when the gateway
    /// forwards the node field without interpreting the identifier.
    /// </summary>
    public bool TryGetAnyNodeLookupSchema([NotNullWhen(true)] out string? schemaName)
    {
        foreach (var value in _sourceSchemaNodeLookupSchemas)
        {
            schemaName = value;
            return true;
        }

        schemaName = null;
        return false;
    }

    void INeedsCompletion.Complete(FusionSchemaDefinition schema, CompositeSchemaBuilderContext context)
    {
        if (!schema.Types.TryGetType<IInterfaceTypeDefinition>("Node", out var nodeType)
            || !schema.QueryType.Fields.TryGetField("node", allowInaccessibleFields: true, out var nodeField)
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
                        && l is { FieldName: "node", IsInternal: false });

            if (nodeLookup is null)
            {
                continue;
            }

            lookup.Add(possibleType.Name, nodeLookup.SchemaName);
        }

        _schemaByType = lookup.ToFrozenDictionary();

        var sourceSchemaNodeLookupSchemas = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var possibleType in schema.GetPossibleTypes(nodeType))
        {
            foreach (var nodeLookup in schema.GetPossibleLookups(possibleType))
            {
                if (nodeLookup.Fields is [PathNode { PathSegment.FieldName.Value: "id" }]
                    && nodeLookup is { FieldName: "node", IsInternal: false }
                    && nodeLookup.FieldType == nodeType
                    && nodeLookup.Path.IsDefaultOrEmpty
                    && possibleType.Sources.TryGetMember(
                        nodeLookup.SchemaName,
                        out var sourceType)
                    && sourceType.Implements.Contains(nodeType.Name))
                {
                    sourceSchemaNodeLookupSchemas.Add(nodeLookup.SchemaName);
                }
            }
        }

        _sourceSchemaNodeLookupSchemas = [.. sourceSchemaNodeLookupSchemas];
    }
}
