using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
namespace HotChocolate.Fusion.ApolloFederation;

/// <summary>
/// Generates <c>@lookup</c> query fields for each resolvable entity key.
/// </summary>
internal static class GenerateLookupFields
{
    /// <summary>
    /// Applies the lookup field generation to the schema.
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

        var internalDef = new MutableDirectiveDefinition("internal");
        var lookupDef = new MutableDirectiveDefinition("lookup");
        var isDef = new MutableDirectiveDefinition("is");

        foreach (var type in schema.Types)
        {
            if (type is not MutableComplexTypeDefinition complexType)
            {
                continue;
            }

            foreach (var keyDirective in complexType.Directives["key"])
            {
                if (!keyDirective.Arguments.TryGetValue("fields", out var fieldsValue)
                    || fieldsValue is not StringValueNode fieldsString)
                {
                    continue;
                }

                var resolvable = true;

                if (keyDirective.Arguments.TryGetValue("resolvable", out var resolvableValue)
                    && resolvableValue is BooleanValueNode boolValue)
                {
                    resolvable = boolValue.Value;
                }

                if (!resolvable)
                {
                    continue;
                }

                var field = GenerateLookupField(
                    schema,
                    complexType,
                    fieldsString.Value,
                    internalDef,
                    lookupDef,
                    isDef);

                if (field is not null)
                {
                    schema.QueryType.Fields.Add(field);
                }
            }
        }
    }

    private static MutableOutputFieldDefinition? GenerateLookupField(
        MutableSchemaDefinition schema,
        MutableComplexTypeDefinition complexType,
        string fieldsSelection,
        MutableDirectiveDefinition internalDef,
        MutableDirectiveDefinition lookupDef,
        MutableDirectiveDefinition isDef)
    {
        SelectionSetNode selectionSet;

        try
        {
            selectionSet = Utf8GraphQLParser.Syntax.ParseSelectionSet(
                "{ " + fieldsSelection + " }");
        }
        catch (SyntaxException)
        {
            return null;
        }

        var leafFields = new List<LeafFieldInfo>();
        ExtractLeafFields(selectionSet, [], leafFields);

        if (leafFields.Count == 0)
        {
            return null;
        }

        var nameParts = new List<string>();
        var fieldName = ToCamelCase(complexType.Name) + "By";

        // Build a temporary field to set as DeclaringMember on arguments.
        var lookupField = new MutableOutputFieldDefinition(
            "placeholder",
            complexType)
        {
            DeclaringMember = schema.QueryType
        };

        foreach (var leaf in leafFields)
        {
            var fieldType = ResolveLeafFieldType(leaf, complexType, schema);

            if (fieldType is null)
            {
                continue;
            }

            // Make the type NonNull.
            var nonNullType = EnsureNonNull(fieldType);

            if (nonNullType is not IInputType inputType)
            {
                continue;
            }

            var argument = new MutableInputFieldDefinition(leaf.ArgumentName, inputType)
            {
                DeclaringMember = lookupField
            };

            // If the field has a nested path, add @is directive.
            if (leaf.Path.Count > 0)
            {
                var fieldPath = BuildFieldPath(leaf);
                argument.Directives.Add(
                    new Directive(isDef, new ArgumentAssignment("field", fieldPath)));
            }

            lookupField.Arguments.Add(argument);
            nameParts.Add(ToPascalCase(leaf.ArgumentName));
        }

        if (lookupField.Arguments.Count == 0)
        {
            return null;
        }

        fieldName += string.Join("And", nameParts);

        // Update the field name now that we know it.
        lookupField.Name = fieldName;

        lookupField.Directives.Add(new Directive(internalDef));
        lookupField.Directives.Add(new Directive(lookupDef));

        return lookupField;
    }

    private static IType? ResolveLeafFieldType(
        LeafFieldInfo leaf,
        MutableComplexTypeDefinition owningType,
        MutableSchemaDefinition schema)
    {
        if (leaf.Path.Count == 0)
        {
            // Simple field: look up directly.
            if (owningType.Fields.TryGetField(leaf.FieldName, out var field))
            {
                return field.Type;
            }

            return null;
        }

        // Nested field: walk the path.
        var currentType = owningType;

        foreach (var pathSegment in leaf.Path)
        {
            if (!currentType.Fields.TryGetField(pathSegment, out var pathField))
            {
                return null;
            }

            var namedType = pathField.Type.NamedType();

            if (!schema.Types.TryGetType<MutableComplexTypeDefinition>(namedType.Name, out var nestedType))
            {
                return null;
            }

            currentType = nestedType;
        }

        // Now look up the final leaf field.
        if (currentType.Fields.TryGetField(leaf.FieldName, out var leafField))
        {
            return leafField.Type;
        }

        return null;
    }

    private static void ExtractLeafFields(
        SelectionSetNode selectionSet,
        List<string> parentPath,
        List<LeafFieldInfo> results)
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
                // Nested field: recurse with the current field added to the path.
                var nestedPath = new List<string>(parentPath) { fieldName };
                ExtractLeafFields(fieldNode.SelectionSet, nestedPath, results);
            }
            else
            {
                // Leaf field.
                var argumentName = parentPath.Count > 0
                    ? BuildArgumentName(parentPath, fieldName)
                    : fieldName;

                results.Add(new LeafFieldInfo
                {
                    FieldName = fieldName,
                    ArgumentName = argumentName,
                    Path = parentPath
                });
            }
        }
    }

    private static string BuildArgumentName(List<string> path, string fieldName)
    {
        // e.g., path=["variation"], fieldName="id" => "variationId"
        var result = path[0];

        for (var i = 1; i < path.Count; i++)
        {
            result += ToPascalCase(path[i]);
        }

        result += ToPascalCase(fieldName);
        return result;
    }

    private static string BuildFieldPath(LeafFieldInfo leaf)
    {
        // Build something like "variation { id }"
        var result = string.Empty;

        for (var i = 0; i < leaf.Path.Count; i++)
        {
            if (i > 0)
            {
                result += " { ";
            }

            result += leaf.Path[i];
        }

        result += " { " + leaf.FieldName + " }";

        for (var i = 1; i < leaf.Path.Count; i++)
        {
            result += " }";
        }

        return result;
    }

    private static IType EnsureNonNull(IType type)
    {
        if (type.Kind is TypeKind.NonNull)
        {
            return type;
        }

        return new NonNullType(type);
    }

    private static string ToCamelCase(string value)
    {
        if (value.Length == 0)
        {
            return value;
        }

        if (char.IsLower(value[0]))
        {
            return value;
        }

        return char.ToLowerInvariant(value[0]) + value[1..];
    }

    private static string ToPascalCase(string value)
    {
        if (value.Length == 0)
        {
            return value;
        }

        if (char.IsUpper(value[0]))
        {
            return value;
        }

        return char.ToUpperInvariant(value[0]) + value[1..];
    }

    private sealed class LeafFieldInfo
    {
        public required string FieldName { get; init; }

        public required string ArgumentName { get; init; }

        public required List<string> Path { get; init; }
    }
}
