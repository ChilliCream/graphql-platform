namespace HotChocolate.Types.Introspection;

/// <summary>
/// Walks an <see cref="ISchemaDefinition"/> and produces <see cref="BM25Document"/> entries
/// for all indexable schema elements, along with a reverse adjacency map for path-to-root traversal.
/// </summary>
internal static class SchemaIndexer
{
    /// <summary>
    /// Indexes the specified schema definition, producing documents for the BM25 index
    /// and a reverse adjacency map for path-to-root queries.
    /// </summary>
    /// <param name="schema">
    /// The schema definition to index.
    /// </param>
    /// <returns>
    /// A tuple containing the list of documents and the reverse adjacency map.
    /// The reverse adjacency map maps a type name to the list of (declaringTypeName, fieldName) pairs
    /// where a field on the declaring type returns a value of that type.
    /// </returns>
    public static (List<BM25Document> Documents, Dictionary<string, List<TypeFieldReference>> ReverseMap) Index(
        ISchemaDefinition schema)
    {
        var documents = new List<BM25Document>();
        var reverseMap = new Dictionary<string, List<TypeFieldReference>>(StringComparer.Ordinal);

        foreach (var type in schema.Types)
        {
            // Skip introspection types (names starting with "__").
            if (type.IsIntrospectionType)
            {
                continue;
            }

            // Index the type itself.
            documents.Add(new BM25Document(
                new SchemaCoordinate(type.Name),
                BuildText(type.Name, type.Description)));

            switch (type)
            {
                case IComplexTypeDefinition complexType:
                    IndexComplexTypeFields(complexType, documents, reverseMap);
                    break;

                case IEnumTypeDefinition enumType:
                    IndexEnumValues(enumType, documents);
                    break;

                case IInputObjectTypeDefinition inputObjectType:
                    IndexInputObjectFields(inputObjectType, documents);
                    break;
            }
        }

        // Index directive definitions.
        foreach (var directive in schema.DirectiveDefinitions)
        {
            // Skip introspection directives.
            if (directive.Name.StartsWith("__", StringComparison.Ordinal))
            {
                continue;
            }

            documents.Add(new BM25Document(
                new SchemaCoordinate(directive.Name, ofDirective: true),
                BuildText(directive.Name, directive.Description)));
        }

        return (documents, reverseMap);
    }

    private static void IndexComplexTypeFields(
        IComplexTypeDefinition complexType,
        List<BM25Document> documents,
        Dictionary<string, List<TypeFieldReference>> reverseMap)
    {
        foreach (var field in complexType.Fields)
        {
            // Skip introspection fields.
            if (field.IsIntrospectionField)
            {
                continue;
            }

            documents.Add(new BM25Document(
                new SchemaCoordinate(complexType.Name, field.Name),
                BuildText(field.Name, field.Description)));

            // Build reverse adjacency: the field's return type points back to this type.
            var returnType = field.Type.NamedType();

            if (!reverseMap.TryGetValue(returnType.Name, out var references))
            {
                references = [];
                reverseMap[returnType.Name] = references;
            }

            references.Add(new TypeFieldReference(complexType.Name, field.Name));
        }
    }

    private static void IndexEnumValues(
        IEnumTypeDefinition enumType,
        List<BM25Document> documents)
    {
        foreach (var value in enumType.Values)
        {
            documents.Add(new BM25Document(
                new SchemaCoordinate(enumType.Name, value.Name),
                BuildText(value.Name, value.Description)));
        }
    }

    private static void IndexInputObjectFields(
        IInputObjectTypeDefinition inputObjectType,
        List<BM25Document> documents)
    {
        foreach (var field in inputObjectType.Fields)
        {
            documents.Add(new BM25Document(
                new SchemaCoordinate(inputObjectType.Name, field.Name),
                BuildText(field.Name, field.Description)));
        }
    }

    private static string BuildText(string name, string? description)
    {
        if (string.IsNullOrEmpty(description))
        {
            return name;
        }

        return string.Concat(name, " ", description);
    }

    /// <summary>
    /// Represents a reference from a type's field back to that type,
    /// used in the reverse adjacency map for path-to-root traversal.
    /// </summary>
    internal readonly record struct TypeFieldReference(string TypeName, string FieldName);
}
