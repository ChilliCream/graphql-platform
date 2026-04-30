using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Serialization;

/// <summary>
/// Rewrites a GraphQL schema document by stripping non-null wrappers from
/// applicable output fields and applying the @semanticNonNull directive to
/// communicate the original nullability intent.
/// </summary>
internal static class SemanticNonNullSchemaRewriter
{
    private const string MutationTypeName = "Mutation";
    private const string PageInfoTypeName = "PageInfo";
    private const string CollectionSegmentInfoTypeName = "CollectionSegmentInfo";
    private const string NodeInterfaceName = "Node";
    private const string IdFieldName = "id";

    /// <summary>
    /// Rewrites the supplied schema document and returns a new document that
    /// uses the @semanticNonNull directive in place of non-null type wrappers
    /// on object and interface fields.
    /// </summary>
    /// <param name="schema">
    /// The schema document to rewrite.
    /// </param>
    /// <returns>
    /// The rewritten schema document.
    /// </returns>
    public static DocumentNode Rewrite(DocumentNode schema)
    {
        var mutationTypeName = ResolveMutationTypeName(schema);
        var definitions = new List<IDefinitionNode>(schema.Definitions.Count);
        var anyRewritten = false;

        foreach (var definition in schema.Definitions)
        {
            if (definition is ObjectTypeDefinitionNode objectType)
            {
                if (ShouldSkipObjectType(objectType, mutationTypeName))
                {
                    definitions.Add(objectType);
                    continue;
                }

                var rewrittenObject = RewriteFields(objectType.Fields, out var rewritten);
                if (rewritten)
                {
                    anyRewritten = true;
                    definitions.Add(objectType.WithFields(rewrittenObject));
                }
                else
                {
                    definitions.Add(objectType);
                }
            }
            else if (definition is InterfaceTypeDefinitionNode interfaceType)
            {
                if (interfaceType.Name.Value == NodeInterfaceName)
                {
                    definitions.Add(interfaceType);
                    continue;
                }

                var rewrittenInterface = RewriteFields(interfaceType.Fields, out var rewritten);
                if (rewritten)
                {
                    anyRewritten = true;
                    definitions.Add(interfaceType.WithFields(rewrittenInterface));
                }
                else
                {
                    definitions.Add(interfaceType);
                }
            }
            else
            {
                definitions.Add(definition);
            }
        }

        if (!anyRewritten)
        {
            return schema;
        }

        if (!HasDirectiveDefinition(definitions, DirectiveNames.SemanticNonNull.Name))
        {
            var directive = CreateSemanticNonNullDirectiveDefinition();
            var insertionIndex = FindDirectiveInsertionIndex(definitions, directive.Name.Value);
            definitions.Insert(insertionIndex, directive);
        }

        return schema.WithDefinitions(definitions);
    }

    private static bool HasDirectiveDefinition(List<IDefinitionNode> definitions, string directiveName)
    {
        for (var i = 0; i < definitions.Count; i++)
        {
            if (definitions[i] is DirectiveDefinitionNode existing
                && existing.Name.Value == directiveName)
            {
                return true;
            }
        }

        return false;
    }

    private static int FindDirectiveInsertionIndex(List<IDefinitionNode> definitions, string directiveName)
    {
        var firstDirectiveIdx = -1;
        var lastDirectiveIdx = -1;

        for (var i = 0; i < definitions.Count; i++)
        {
            if (definitions[i] is DirectiveDefinitionNode)
            {
                if (firstDirectiveIdx < 0)
                {
                    firstDirectiveIdx = i;
                }
                lastDirectiveIdx = i;
            }
        }

        if (firstDirectiveIdx >= 0)
        {
            for (var i = firstDirectiveIdx; i <= lastDirectiveIdx; i++)
            {
                if (definitions[i] is DirectiveDefinitionNode existing
                    && string.CompareOrdinal(directiveName, existing.Name.Value) < 0)
                {
                    return i;
                }
            }

            return lastDirectiveIdx + 1;
        }

        var firstScalarIdx = -1;
        var afterLastEnumIdx = -1;

        for (var i = 0; i < definitions.Count; i++)
        {
            if (definitions[i] is EnumTypeDefinitionNode)
            {
                afterLastEnumIdx = i + 1;
            }
            else if (firstScalarIdx < 0 && definitions[i] is ScalarTypeDefinitionNode)
            {
                firstScalarIdx = i;
            }
        }

        if (firstScalarIdx >= 0)
        {
            return firstScalarIdx;
        }

        if (afterLastEnumIdx >= 0)
        {
            return afterLastEnumIdx;
        }

        return definitions.Count;
    }

    private static string ResolveMutationTypeName(DocumentNode schema)
    {
        foreach (var definition in schema.Definitions)
        {
            if (definition is SchemaDefinitionNode schemaDefinition)
            {
                foreach (var operationType in schemaDefinition.OperationTypes)
                {
                    if (operationType.Operation == OperationType.Mutation)
                    {
                        return operationType.Type.Name.Value;
                    }
                }

                return MutationTypeName;
            }
        }

        return MutationTypeName;
    }

    private static bool ShouldSkipObjectType(ObjectTypeDefinitionNode objectType, string mutationTypeName)
    {
        var name = objectType.Name.Value;

        if (name.StartsWith("__", StringComparison.Ordinal))
        {
            return true;
        }

        if (name == PageInfoTypeName || name == CollectionSegmentInfoTypeName)
        {
            return true;
        }

        if (name == mutationTypeName)
        {
            return true;
        }

        return false;
    }

    private static IReadOnlyList<FieldDefinitionNode> RewriteFields(
        IReadOnlyList<FieldDefinitionNode> fields,
        out bool rewritten)
    {
        rewritten = false;
        var result = new List<FieldDefinitionNode>(fields.Count);

        foreach (var field in fields)
        {
            var fieldName = field.Name.Value;

            if (fieldName.StartsWith("__", StringComparison.Ordinal) || fieldName == IdFieldName)
            {
                result.Add(field);
                continue;
            }

            var levels = GetSemanticNonNullLevels(field.Type);

            if (levels.Count < 1)
            {
                result.Add(field);
                continue;
            }

            rewritten = true;
            var nullableType = ToNullableType(field.Type);
            var newDirective = CreateSemanticNonNullDirective(levels);
            var directives = new List<DirectiveNode>(field.Directives.Count + 1);
            var replaced = false;

            foreach (var existing in field.Directives)
            {
                if (!replaced && existing.Name.Value == DirectiveNames.SemanticNonNull.Name)
                {
                    directives.Add(newDirective);
                    replaced = true;
                }
                else
                {
                    directives.Add(existing);
                }
            }

            if (!replaced)
            {
                directives.Add(newDirective);
            }

            result.Add(field.WithType(nullableType).WithDirectives(directives));
        }

        return result;
    }

    private static List<int> GetSemanticNonNullLevels(ITypeNode type)
    {
        var levels = new List<int>();
        var current = type;
        var index = 0;

        while (true)
        {
            if (current is ListTypeNode listType)
            {
                index++;
                current = listType.Type;
            }
            else if (current is NonNullTypeNode nonNullType)
            {
                if (!levels.Contains(index))
                {
                    levels.Add(index);
                }
                current = nonNullType.Type;
            }
            else
            {
                break;
            }
        }

        return levels;
    }

    private static ITypeNode ToNullableType(ITypeNode type)
    {
        if (type is ListTypeNode listType)
        {
            return new ListTypeNode(ToNullableType(listType.Type));
        }

        if (type is NonNullTypeNode nonNullType)
        {
            return ToNullableType(nonNullType.Type);
        }

        return type;
    }

    private static DirectiveNode CreateSemanticNonNullDirective(List<int> levels)
    {
        if (levels.Count == 1 && levels[0] == 0)
        {
            return new DirectiveNode(DirectiveNames.SemanticNonNull.Name);
        }

        var items = new IValueNode[levels.Count];
        for (var i = 0; i < levels.Count; i++)
        {
            items[i] = new IntValueNode(levels[i]);
        }

        return new DirectiveNode(
            DirectiveNames.SemanticNonNull.Name,
            new ArgumentNode(DirectiveNames.SemanticNonNull.Arguments.Levels, new ListValueNode(items)));
    }

    private static DirectiveDefinitionNode CreateSemanticNonNullDirectiveDefinition()
    {
        var levelsType = new ListTypeNode(new NonNullTypeNode(new NamedTypeNode("Int")));
        var defaultValue = new ListValueNode(new IntValueNode(0));

        var argument = new InputValueDefinitionNode(
            location: null,
            name: new NameNode(DirectiveNames.SemanticNonNull.Arguments.Levels),
            description: null,
            type: levelsType,
            defaultValue: defaultValue,
            directives: Array.Empty<DirectiveNode>());

        return new DirectiveDefinitionNode(
            location: null,
            name: new NameNode(DirectiveNames.SemanticNonNull.Name),
            description: null,
            isRepeatable: false,
            arguments: new[] { argument },
            locations: new[] { new NameNode("FIELD_DEFINITION") });
    }
}
