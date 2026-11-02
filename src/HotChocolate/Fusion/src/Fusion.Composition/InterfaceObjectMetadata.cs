using HotChocolate.Fusion.Extensions;
using HotChocolate.Language;
using HotChocolate.Types.Mutable;
using ArgumentNames = HotChocolate.Fusion.WellKnownArgumentNames;

namespace HotChocolate.Fusion;

/// <summary>
/// Shared helpers for reasoning about <c>@interfaceObject</c> stand-ins across the source schemas.
/// A stand-in is an object type annotated with <c>@interfaceObject</c> whose name matches an
/// interface defined elsewhere; its non-key fields are default implementations for that interface.
/// </summary>
internal static class InterfaceObjectMetadata
{
    /// <summary>
    /// Gets every <c>@interfaceObject</c> stand-in named <paramref name="interfaceName"/> across the
    /// source schemas, together with the schema that declares it.
    /// </summary>
    public static IEnumerable<(MutableObjectTypeDefinition StandIn, MutableSchemaDefinition Schema)> GetStandIns(
        IEnumerable<MutableSchemaDefinition> schemas,
        string interfaceName)
    {
        foreach (var schema in schemas)
        {
            if (schema.Types.TryGetType(interfaceName, out MutableObjectTypeDefinition? standIn)
                && standIn.Directives.ContainsName(WellKnownDirectiveNames.InterfaceObject))
            {
                yield return (standIn, schema);
            }
        }
    }

    /// <summary>
    /// Gets the top-level field names selected by the <c>@key</c> directives on a stand-in.
    /// </summary>
    public static HashSet<string> GetKeyFieldNames(MutableObjectTypeDefinition standIn)
    {
        var keyFieldNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var keyDirective in standIn.Directives.AsEnumerable())
        {
            if (keyDirective.Name != WellKnownDirectiveNames.Key
                || !keyDirective.Arguments.TryGetValue(ArgumentNames.Fields, out var value)
                || value is not StringValueNode fields)
            {
                continue;
            }

            try
            {
                var selectionSet = Utf8GraphQLParser.Syntax.ParseSelectionSet($"{{ {fields.Value} }}");

                foreach (var selection in selectionSet.Selections)
                {
                    if (selection is FieldNode fieldNode)
                    {
                        keyFieldNames.Add(fieldNode.Name.Value);
                    }
                }
            }
            catch (SyntaxException)
            {
                // A malformed key is reported by the key validation rules; ignore it here.
            }
        }

        return keyFieldNames;
    }

    /// <summary>
    /// Gets the default field names an interface's stand-ins contribute: the non-key, non-internal,
    /// non-inaccessible fields declared on any stand-in named <paramref name="interfaceName"/>.
    /// </summary>
    public static HashSet<string> DefaultFields(
        IEnumerable<MutableSchemaDefinition> schemas,
        string interfaceName)
    {
        var fieldNames = new HashSet<string>(StringComparer.Ordinal);
        var keyFieldNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (standIn, _) in GetStandIns(schemas, interfaceName))
        {
            keyFieldNames.UnionWith(GetKeyFieldNames(standIn));

            foreach (var field in standIn.Fields)
            {
                if (!field.IsInternal && !field.IsInaccessible)
                {
                    fieldNames.Add(field.Name);
                }
            }
        }

        fieldNames.ExceptWith(keyFieldNames);

        return fieldNames;
    }
}
