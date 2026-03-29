using HotChocolate.Language;

namespace HotChocolate.Fusion.ApolloFederation;

/// <summary>
/// Generates <c>@lookup</c> query fields for each resolvable entity key.
/// </summary>
internal static class GenerateLookupFields
{
    /// <summary>
    /// Applies the lookup field generation to the document.
    /// </summary>
    /// <param name="document">
    /// The document to transform.
    /// </param>
    /// <param name="analysis">
    /// The analysis result containing entity key and field type metadata.
    /// </param>
    /// <returns>
    /// A new document with generated lookup fields on the Query type.
    /// </returns>
    public static DocumentNode Apply(DocumentNode document, AnalysisResult analysis)
    {
        var lookupFields = new List<FieldDefinitionNode>();

        foreach (var (typeName, keys) in analysis.EntityKeys)
        {
            foreach (var key in keys)
            {
                if (!key.Resolvable)
                {
                    continue;
                }

                var field = GenerateLookupField(typeName, key, analysis);

                if (field is not null)
                {
                    lookupFields.Add(field);
                }
            }
        }

        if (lookupFields.Count == 0)
        {
            return document;
        }

        return AppendFieldsToQueryType(document, analysis.QueryTypeName, lookupFields);
    }

    private static FieldDefinitionNode? GenerateLookupField(
        string typeName,
        EntityKeyInfo key,
        AnalysisResult analysis)
    {
        SelectionSetNode selectionSet;

        try
        {
            selectionSet = Utf8GraphQLParser.Syntax.ParseSelectionSet(
                "{ " + key.Fields + " }");
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

        if (!analysis.TypeFieldTypes.TryGetValue(typeName, out var fieldTypes))
        {
            return null;
        }

        var arguments = new List<InputValueDefinitionNode>();
        var nameParts = new List<string>();

        foreach (var leaf in leafFields)
        {
            var argumentName = leaf.ArgumentName;
            var fieldType = ResolveLeafFieldType(leaf, typeName, analysis);

            if (fieldType is null)
            {
                continue;
            }

            // Make the type NonNull.
            var nonNullType = EnsureNonNull(fieldType);

            var argumentDirectives = new List<DirectiveNode>();

            // If the field has a nested path, add @is directive.
            if (leaf.Path.Count > 0)
            {
                var fieldPath = BuildFieldPath(leaf);
                argumentDirectives.Add(
                    new DirectiveNode(
                        "is",
                        new ArgumentNode("field", new StringValueNode(fieldPath))));
            }

            arguments.Add(
                new InputValueDefinitionNode(
                    null,
                    new NameNode(argumentName),
                    null,
                    nonNullType,
                    null,
                    argumentDirectives));

            nameParts.Add(ToPascalCase(argumentName));
        }

        if (arguments.Count == 0)
        {
            return null;
        }

        var fieldName = ToCamelCase(typeName) + "By" + string.Join("And", nameParts);

        return new FieldDefinitionNode(
            null,
            new NameNode(fieldName),
            null,
            arguments,
            new NamedTypeNode(typeName),
            [new DirectiveNode("internal"), new DirectiveNode("lookup")]);
    }

    private static ITypeNode? ResolveLeafFieldType(
        LeafFieldInfo leaf,
        string typeName,
        AnalysisResult analysis)
    {
        if (leaf.Path.Count == 0)
        {
            // Simple field: look up directly.
            if (analysis.TypeFieldTypes.TryGetValue(typeName, out var fieldTypes)
                && fieldTypes.TryGetValue(leaf.FieldName, out var fieldType))
            {
                return fieldType;
            }

            return null;
        }

        // Nested field: walk the path.
        var currentTypeName = typeName;

        foreach (var pathSegment in leaf.Path)
        {
            if (!analysis.TypeFieldTypes.TryGetValue(currentTypeName, out var pathFieldTypes)
                || !pathFieldTypes.TryGetValue(pathSegment, out var pathFieldType))
            {
                return null;
            }

            currentTypeName = GetNamedTypeName(pathFieldType);

            if (currentTypeName is null)
            {
                return null;
            }
        }

        // Now look up the final leaf field.
        if (analysis.TypeFieldTypes.TryGetValue(currentTypeName, out var leafFieldTypes)
            && leafFieldTypes.TryGetValue(leaf.FieldName, out var leafFieldType))
        {
            return leafFieldType;
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
                ExtractLeafFields(fieldNode.SelectionSet!, nestedPath, results);
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

    private static DocumentNode AppendFieldsToQueryType(
        DocumentNode document,
        string queryTypeName,
        List<FieldDefinitionNode> lookupFields)
    {
        var definitions = new List<IDefinitionNode>(document.Definitions.Count);

        foreach (var definition in document.Definitions)
        {
            if (definition is ObjectTypeDefinitionNode objectType
                && objectType.Name.Value.Equals(queryTypeName, StringComparison.Ordinal))
            {
                var allFields = new List<FieldDefinitionNode>(
                    objectType.Fields.Count + lookupFields.Count);

                allFields.AddRange(objectType.Fields);
                allFields.AddRange(lookupFields);

                definitions.Add(objectType.WithFields(allFields));
            }
            else
            {
                definitions.Add(definition);
            }
        }

        return document.WithDefinitions(definitions);
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
