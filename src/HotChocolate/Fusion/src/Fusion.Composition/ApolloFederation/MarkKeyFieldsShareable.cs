using HotChocolate.Fusion.Extensions;
using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.ApolloFederation;

/// <summary>
/// Marks fields that participate in Apollo Federation <c>@key</c> selections
/// as <c>@shareable</c> in source schemas that contribute those fields.
/// </summary>
internal static class MarkKeyFieldsShareable
{
    /// <summary>
    /// Applies <c>@shareable</c> to fields in <paramref name="schema" /> when
    /// the same type and field participate in a <c>@key</c> selection in any
    /// source schema in the composition.
    /// </summary>
    /// <param name="schema">
    /// The mutable schema definition to transform in place.
    /// </param>
    /// <param name="schemas">
    /// All mutable source schema definitions in the composition.
    /// </param>
    public static void Apply(
        MutableSchemaDefinition schema,
        IEnumerable<MutableSchemaDefinition> schemas)
    {
        var keyReferences = CollectKeyReferences(schemas);

        foreach (var type in schema.Types)
        {
            // '@shareable' is only meaningful on object type fields. Interface types can carry
            // a '@key' (entity interfaces), but their field definitions must never be stamped
            // '@shareable' or the composition's shareable-usage validation rejects them.
            if (type is not MutableObjectTypeDefinition objectType)
            {
                continue;
            }

            foreach (var field in objectType.Fields)
            {
                if (!keyReferences.Contains((objectType.Name, field.Name)))
                {
                    continue;
                }

                if (field.Directives.ContainsName(FederationDirectiveNames.Shareable)
                    || field.Directives.ContainsName(FederationDirectiveNames.External))
                {
                    continue;
                }

                field.ApplyShareableDirective();
            }
        }
    }

    private static HashSet<(string TypeName, string FieldName)> CollectKeyReferences(
        IEnumerable<MutableSchemaDefinition> schemas)
    {
        var referenced = new HashSet<(string, string)>();

        foreach (var schema in schemas)
        {
            foreach (var type in schema.Types)
            {
                if (type is not MutableComplexTypeDefinition complexType)
                {
                    continue;
                }

                foreach (var keyDirective in complexType.Directives[FederationDirectiveNames.Key])
                {
                    if (!keyDirective.Arguments.TryGetValue("fields", out var fieldsValue)
                        || fieldsValue is not StringValueNode fieldsString)
                    {
                        continue;
                    }

                    SelectionSetNode selectionSet;

                    try
                    {
                        selectionSet = Utf8GraphQLParser.Syntax.ParseSelectionSet(
                            "{ " + fieldsString.Value + " }");
                    }
                    catch (SyntaxException)
                    {
                        continue;
                    }

                    CollectReferencedFields(selectionSet, complexType, schema, referenced);
                }
            }
        }

        return referenced;
    }

    private static void CollectReferencedFields(
        SelectionSetNode selectionSet,
        MutableComplexTypeDefinition currentType,
        MutableSchemaDefinition schema,
        HashSet<(string, string)> referenced)
    {
        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode fieldNode:
                    referenced.Add((currentType.Name, fieldNode.Name.Value));

                    if (fieldNode.SelectionSet?.Selections.Count > 0
                        && currentType.Fields.TryGetField(
                            fieldNode.Name.Value, out var nestedField))
                    {
                        var nestedNamedType = nestedField.Type.NamedType();

                        if (schema.Types.TryGetType<MutableComplexTypeDefinition>(
                                nestedNamedType.Name, out var nestedType))
                        {
                            CollectReferencedFields(
                                fieldNode.SelectionSet, nestedType, schema, referenced);
                        }
                    }

                    break;

                case InlineFragmentNode inlineFragment
                    when inlineFragment.TypeCondition is not null:

                    if (schema.Types.TryGetType<MutableComplexTypeDefinition>(
                            inlineFragment.TypeCondition.Name.Value, out var fragmentType))
                    {
                        CollectReferencedFields(
                            inlineFragment.SelectionSet, fragmentType, schema, referenced);
                    }

                    break;
            }
        }
    }
}
