using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
namespace HotChocolate.Fusion.ApolloFederation;

/// <summary>
/// Transforms <c>@requires</c> directives into <c>@require</c> field arguments
/// per the Composite Schema specification.
/// </summary>
internal static class TransformRequiresToRequire
{
    /// <summary>
    /// Applies the requires-to-require transformation on the schema.
    /// </summary>
    /// <param name="schema">
    /// The mutable schema definition to transform in place.
    /// </param>
    public static void Apply(MutableSchemaDefinition schema)
    {
        var requireDef = new MutableDirectiveDefinition("require");

        foreach (var type in schema.Types.OfType<MutableObjectTypeDefinition>())
        {
            foreach (var field in type.Fields)
            {
                var requiresDirective = field.Directives.FirstOrDefault(
                    FederationDirectiveNames.Requires);

                if (requiresDirective is null)
                {
                    continue;
                }

                if (!requiresDirective.Arguments.TryGetValue("fields", out var fieldsValue)
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

                ExtractRequireArguments(
                    selectionSet,
                    [],
                    type,
                    schema,
                    field,
                    requireDef);

                // Remove the @requires directive from the field.
                field.Directives.Remove(requiresDirective);
            }
        }
    }

    private static void ExtractRequireArguments(
        SelectionSetNode selectionSet,
        List<string> parentPath,
        MutableComplexTypeDefinition currentType,
        MutableSchemaDefinition schema,
        MutableOutputFieldDefinition targetField,
        MutableDirectiveDefinition requireDef)
    {
        foreach (var selection in selectionSet.Selections)
        {
            if (selection is not FieldNode fieldNode)
            {
                continue;
            }

            var fieldName = fieldNode.Name.Value;

            if (fieldNode.SelectionSet?.Selections.Count > 0)
            {
                // Nested selection: recurse.
                if (!currentType.Fields.TryGetField(fieldName, out var pathField))
                {
                    continue;
                }

                var namedType = pathField.Type.NamedType();

                if (!schema.Types.TryGetType<MutableComplexTypeDefinition>(
                        namedType.Name, out var nestedType))
                {
                    continue;
                }

                var nestedPath = new List<string>(parentPath) { fieldName };

                ExtractRequireArguments(
                    fieldNode.SelectionSet!,
                    nestedPath,
                    nestedType,
                    schema,
                    targetField,
                    requireDef);
            }
            else
            {
                // Leaf field: generate an argument.
                if (!currentType.Fields.TryGetField(fieldName, out var sourceField))
                {
                    continue;
                }

                // Mirror the source field's nullability on the generated
                // argument. The composed schema validator compares this
                // argument against the owning source field as-is, so
                // wrapping a nullable source in NonNull here would make
                // every post-merge validation reject the composition.
                if (sourceField.Type is not IInputType inputType)
                {
                    continue;
                }

                string requireFieldValue;

                if (parentPath.Count == 0)
                {
                    requireFieldValue = fieldName;
                }
                else
                {
                    requireFieldValue = BuildFieldPath(parentPath, fieldName);
                }

                var argument = new MutableInputFieldDefinition(fieldName, inputType)
                {
                    DeclaringMember = targetField
                };

                argument.Directives.Add(
                    new Directive(
                        requireDef,
                        new ArgumentAssignment("field", requireFieldValue)));

                targetField.Arguments.Add(argument);
            }
        }
    }

    private static string BuildFieldPath(List<string> path, string fieldName)
    {
        // Build something like "dimension { height }"
        var result = string.Empty;

        for (var i = 0; i < path.Count; i++)
        {
            if (i > 0)
            {
                result += " { ";
            }

            result += path[i];
        }

        result += " { " + fieldName + " }";

        for (var i = 1; i < path.Count; i++)
        {
            result += " }";
        }

        return result;
    }
}
