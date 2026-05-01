using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.ApolloFederation;

/// <summary>
/// Removes fields marked with <c>@external</c> from complex types,
/// preserving those referenced by <c>@provides</c> selections.
/// </summary>
internal static class RemoveExternalFields
{
    /// <summary>
    /// Removes unreferenced <c>@external</c> fields from the schema.
    /// External fields that are the target of a <c>@provides</c> selection
    /// on the same subgraph are kept so the downstream Composite Schema Spec
    /// validator and planner can see them.
    /// </summary>
    /// <param name="schema">
    /// The mutable schema definition to transform in place.
    /// </param>
    public static void Apply(MutableSchemaDefinition schema)
    {
        var referencedFields = CollectProvidesReferences(schema);

        foreach (var type in schema.Types)
        {
            if (type is not MutableComplexTypeDefinition complexType)
            {
                continue;
            }

            var externalFields = new List<MutableOutputFieldDefinition>();

            foreach (var field in complexType.Fields)
            {
                if (field.Directives.ContainsName(FederationDirectiveNames.External)
                    && !referencedFields.Contains((complexType.Name, field.Name)))
                {
                    externalFields.Add(field);
                }
            }

            foreach (var field in externalFields)
            {
                complexType.Fields.Remove(field);
            }
        }
    }

    private static HashSet<(string TypeName, string FieldName)> CollectProvidesReferences(
        MutableSchemaDefinition schema)
    {
        var referenced = new HashSet<(string, string)>();

        foreach (var type in schema.Types)
        {
            if (type is not MutableComplexTypeDefinition complexType)
            {
                continue;
            }

            foreach (var field in complexType.Fields)
            {
                var providesDirective = field.Directives.FirstOrDefault(
                    FederationDirectiveNames.Provides);

                if (providesDirective is null)
                {
                    continue;
                }

                if (!providesDirective.Arguments.TryGetValue("fields", out var fieldsValue)
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

                var namedType = field.Type.NamedType();

                if (!schema.Types.TryGetType<MutableComplexTypeDefinition>(
                        namedType.Name, out var targetType))
                {
                    continue;
                }

                CollectReferencedFields(selectionSet, targetType, schema, referenced);
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
