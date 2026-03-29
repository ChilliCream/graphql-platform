using HotChocolate.Language;

namespace HotChocolate.Fusion.ApolloFederation;

/// <summary>
/// Transforms <c>@requires</c> directives into <c>@require</c> field arguments
/// per the Composite Schema specification.
/// </summary>
internal static class TransformRequiresToRequire
{
    /// <summary>
    /// Applies the requires-to-require transformation on the document.
    /// </summary>
    /// <param name="document">
    /// The document to transform.
    /// </param>
    /// <param name="analysis">
    /// The analysis result containing field type metadata.
    /// </param>
    /// <returns>
    /// A new document with <c>@requires</c> directives replaced by
    /// <c>@require</c> argument directives.
    /// </returns>
    public static DocumentNode Apply(DocumentNode document, AnalysisResult analysis)
    {
        var definitions = new List<IDefinitionNode>(document.Definitions.Count);
        var changed = false;

        foreach (var definition in document.Definitions)
        {
            if (definition is ObjectTypeDefinitionNode objectType)
            {
                var transformed = TransformObjectType(objectType, analysis);

                if (!ReferenceEquals(transformed, objectType))
                {
                    changed = true;
                }

                definitions.Add(transformed);
            }
            else
            {
                definitions.Add(definition);
            }
        }

        if (!changed)
        {
            return document;
        }

        return document.WithDefinitions(definitions);
    }

    private static ObjectTypeDefinitionNode TransformObjectType(
        ObjectTypeDefinitionNode objectType,
        AnalysisResult analysis)
    {
        var typeName = objectType.Name.Value;
        var fields = new List<FieldDefinitionNode>(objectType.Fields.Count);
        var anyFieldChanged = false;

        foreach (var field in objectType.Fields)
        {
            var transformed = TransformField(field, typeName, analysis);

            if (!ReferenceEquals(transformed, field))
            {
                anyFieldChanged = true;
            }

            fields.Add(transformed);
        }

        if (!anyFieldChanged)
        {
            return objectType;
        }

        return objectType.WithFields(fields);
    }

    private static FieldDefinitionNode TransformField(
        FieldDefinitionNode field,
        string typeName,
        AnalysisResult analysis)
    {
        DirectiveNode? requiresDirective = null;

        foreach (var directive in field.Directives)
        {
            if (directive.Name.Value.Equals(
                    FederationDirectiveNames.Requires,
                    StringComparison.Ordinal))
            {
                requiresDirective = directive;
                break;
            }
        }

        if (requiresDirective is null)
        {
            return field;
        }

        var fieldsValue = GetStringArgument(requiresDirective, "fields");

        if (fieldsValue is null)
        {
            return field;
        }

        SelectionSetNode selectionSet;

        try
        {
            selectionSet = Utf8GraphQLParser.Syntax.ParseSelectionSet(
                "{ " + fieldsValue + " }");
        }
        catch (SyntaxException)
        {
            return field;
        }

        if (!analysis.TypeFieldTypes.TryGetValue(typeName, out var fieldTypes))
        {
            return field;
        }

        var newArguments = new List<InputValueDefinitionNode>(field.Arguments);

        ExtractRequireArguments(selectionSet, [], typeName, analysis, newArguments);

        // Remove the @requires directive from the field.
        var newDirectives = new List<DirectiveNode>(field.Directives.Count);

        foreach (var directive in field.Directives)
        {
            if (!directive.Name.Value.Equals(
                    FederationDirectiveNames.Requires,
                    StringComparison.Ordinal))
            {
                newDirectives.Add(directive);
            }
        }

        return field
            .WithArguments(newArguments)
            .WithDirectives(newDirectives);
    }

    private static void ExtractRequireArguments(
        SelectionSetNode selectionSet,
        List<string> parentPath,
        string currentTypeName,
        AnalysisResult analysis,
        List<InputValueDefinitionNode> arguments)
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
                var nestedTypeName = ResolveFieldTypeName(currentTypeName, fieldName, analysis);

                if (nestedTypeName is null)
                {
                    continue;
                }

                var nestedPath = new List<string>(parentPath) { fieldName };
                ExtractRequireArguments(
                    fieldNode.SelectionSet!,
                    nestedPath,
                    nestedTypeName,
                    analysis,
                    arguments);
            }
            else
            {
                // Leaf field: generate an argument.
                var fieldType = ResolveFieldType(currentTypeName, fieldName, analysis);

                if (fieldType is null)
                {
                    continue;
                }

                var nonNullType = EnsureNonNull(StripNonNull(fieldType));

                string requireFieldValue;

                if (parentPath.Count == 0)
                {
                    requireFieldValue = fieldName;
                }
                else
                {
                    requireFieldValue = BuildFieldPath(parentPath, fieldName);
                }

                var requireDirective = new DirectiveNode(
                    "require",
                    new ArgumentNode("field", new StringValueNode(requireFieldValue)));

                arguments.Add(
                    new InputValueDefinitionNode(
                        null,
                        new NameNode(fieldName),
                        null,
                        nonNullType,
                        null,
                        [requireDirective]));
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

    private static string? ResolveFieldTypeName(
        string typeName,
        string fieldName,
        AnalysisResult analysis)
    {
        var fieldType = ResolveFieldType(typeName, fieldName, analysis);

        if (fieldType is null)
        {
            return null;
        }

        return GetNamedTypeName(fieldType);
    }

    private static ITypeNode? ResolveFieldType(
        string typeName,
        string fieldName,
        AnalysisResult analysis)
    {
        if (analysis.TypeFieldTypes.TryGetValue(typeName, out var fieldTypes)
            && fieldTypes.TryGetValue(fieldName, out var fieldType))
        {
            return fieldType;
        }

        return null;
    }

    private static string? GetNamedTypeName(ITypeNode typeNode)
    {
        return typeNode switch
        {
            NamedTypeNode named => named.Name.Value,
            NonNullTypeNode nonNull => GetNamedTypeName(nonNull.Type),
            ListTypeNode list => GetNamedTypeName(list.Type),
            _ => null
        };
    }

    private static ITypeNode StripNonNull(ITypeNode typeNode)
    {
        if (typeNode is NonNullTypeNode nonNull)
        {
            return nonNull.Type;
        }

        return typeNode;
    }

    private static ITypeNode EnsureNonNull(ITypeNode typeNode)
    {
        if (typeNode is NonNullTypeNode)
        {
            return typeNode;
        }

        if (typeNode is INullableTypeNode nullable)
        {
            return new NonNullTypeNode(nullable);
        }

        return typeNode;
    }

    private static string? GetStringArgument(DirectiveNode directive, string argumentName)
    {
        foreach (var argument in directive.Arguments)
        {
            if (argument.Name.Value.Equals(argumentName, StringComparison.Ordinal)
                && argument.Value is StringValueNode stringValue)
            {
                return stringValue.Value;
            }
        }

        return null;
    }
}
