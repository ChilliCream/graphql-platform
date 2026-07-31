using System.Collections.Concurrent;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Types.Completion;

/// <summary>
/// Provides the introspection type definitions that are added to every composite schema.
/// The introspection schema shape depends on the schema options, so each conditional field
/// and argument is declared with the definition it belongs to.
/// </summary>
internal static class IntrospectionSchema
{
    private static readonly ConcurrentDictionary<Shape, DocumentNode> s_documents = new();

    private static readonly InputValueDefinitionNode s_includeDeprecated =
        Argument("includeDeprecated", "Boolean!", BooleanValueNode.False);

    private static readonly InputValueDefinitionNode s_includeOptIn =
        Argument("includeOptIn", "[String!]");

    /// <summary>
    /// Gets the introspection schema document for the specified schema options.
    /// The document is immutable and is shared by every schema with the same shape.
    /// </summary>
    /// <param name="options">
    /// The schema options that determine which introspection fields and arguments are exposed.
    /// </param>
    public static DocumentNode GetDocument(IFusionSchemaOptions options)
        => s_documents.GetOrAdd(Shape.From(options), static shape => CreateDocument(shape));

    private static DocumentNode CreateDocument(Shape shape)
    {
        var optIn = shape.OptInFeatures;

        List<IDefinitionNode> definitions =
        [
            CreateSchemaType(optIn),
            CreateTypeType(optIn),
            CreateTypeKindType(),
            CreateFieldType(optIn),
            CreateInputValueType(optIn),
            CreateEnumValueType(optIn),
            CreateDirectiveType(optIn),
            CreateDirectiveLocationType()
        ];

        if (optIn)
        {
            definitions.Add(CreateOptInFeatureStabilityType());
        }

        if (shape.SemanticIntrospection)
        {
            definitions.Add(CreateSearchResultType());
            definitions.Add(CreateSchemaDefinitionType());
        }

        return new DocumentNode(definitions);
    }

    private static ObjectTypeDefinitionNode CreateSchemaType(bool optIn)
        => ObjectType(
            "__Schema",
            [
                Field("description", "String"),
                Field("types", "[__Type!]!"),
                Field("queryType", "__Type!"),
                Field("mutationType", "__Type"),
                Field("subscriptionType", "__Type"),
                Field("directives", "[__Directive!]!", FilterArguments(optIn)),
                .. When(
                    optIn,
                    Field("optInFeatures", "[String!]"),
                    Field("optInFeatureStability", "[__OptInFeatureStability!]!"))
            ]);

    private static ObjectTypeDefinitionNode CreateTypeType(bool optIn)
    {
        var filterArguments = FilterArguments(optIn);

        return ObjectType(
            "__Type",
            [
                Field("kind", "__TypeKind!"),
                Field("name", "String"),
                Field("description", "String"),
                // may be non-null for custom SCALAR, otherwise null.
                Field("specifiedByURL", "String"),
                // must be non-null for OBJECT and INTERFACE, otherwise null.
                Field("fields", "[__Field!]", filterArguments),
                // must be non-null for OBJECT and INTERFACE, otherwise null.
                Field("interfaces", "[__Type!]"),
                // must be non-null for INTERFACE and UNION, otherwise null.
                Field("possibleTypes", "[__Type!]"),
                // must be non-null for ENUM, otherwise null.
                Field("enumValues", "[__EnumValue!]", filterArguments),
                // must be non-null for INPUT_OBJECT, otherwise null.
                Field("inputFields", "[__InputValue!]", filterArguments),
                // must be non-null for NON_NULL and LIST, otherwise null.
                Field("ofType", "__Type"),
                // must be non-null for INPUT_OBJECT, otherwise null.
                Field("isOneOf", "Boolean")
            ]);
    }

    private static EnumTypeDefinitionNode CreateTypeKindType()
        => EnumType(
            "__TypeKind",
            "SCALAR",
            "OBJECT",
            "INTERFACE",
            "UNION",
            "ENUM",
            "INPUT_OBJECT",
            "LIST",
            "NON_NULL");

    private static ObjectTypeDefinitionNode CreateFieldType(bool optIn)
        => ObjectType(
            "__Field",
            [
                Field("name", "String!"),
                Field("description", "String"),
                Field("args", "[__InputValue!]!", FilterArguments(optIn)),
                Field("type", "__Type!"),
                Field("isDeprecated", "Boolean!"),
                Field("deprecationReason", "String"),
                .. When(optIn, Field("requiresOptIn", "[String!]"))
            ]);

    private static ObjectTypeDefinitionNode CreateInputValueType(bool optIn)
        => ObjectType(
            "__InputValue",
            [
                Field("name", "String!"),
                Field("description", "String"),
                Field("type", "__Type!"),
                Field("defaultValue", "String"),
                Field("isDeprecated", "Boolean!"),
                Field("deprecationReason", "String"),
                .. When(optIn, Field("requiresOptIn", "[String!]"))
            ]);

    private static ObjectTypeDefinitionNode CreateEnumValueType(bool optIn)
        => ObjectType(
            "__EnumValue",
            [
                Field("name", "String!"),
                Field("description", "String"),
                Field("isDeprecated", "Boolean!"),
                Field("deprecationReason", "String"),
                .. When(optIn, Field("requiresOptIn", "[String!]"))
            ]);

    private static ObjectTypeDefinitionNode CreateDirectiveType(bool optIn)
        => ObjectType(
            "__Directive",
            [
                Field("name", "String!"),
                Field("description", "String"),
                Field("isRepeatable", "Boolean!"),
                Field("locations", "[__DirectiveLocation!]!"),
                Field("args", "[__InputValue!]!", FilterArguments(optIn)),
                Field("isDeprecated", "Boolean!"),
                Field("deprecationReason", "String"),
                .. When(optIn, Field("requiresOptIn", "[String!]"))
            ]);

    private static EnumTypeDefinitionNode CreateDirectiveLocationType()
        => EnumType(
            "__DirectiveLocation",
            "QUERY",
            "MUTATION",
            "SUBSCRIPTION",
            "FIELD",
            "FRAGMENT_DEFINITION",
            "FRAGMENT_SPREAD",
            "INLINE_FRAGMENT",
            "VARIABLE_DEFINITION",
            "SCHEMA",
            "SCALAR",
            "OBJECT",
            "FIELD_DEFINITION",
            "ARGUMENT_DEFINITION",
            "INTERFACE",
            "UNION",
            "ENUM",
            "ENUM_VALUE",
            "INPUT_OBJECT",
            "INPUT_FIELD_DEFINITION",
            "DIRECTIVE_DEFINITION");

    private static ObjectTypeDefinitionNode CreateOptInFeatureStabilityType()
        => ObjectType(
            "__OptInFeatureStability",
            [
                Field("feature", "String!"),
                Field("stability", "String!")
            ]);

    private static ObjectTypeDefinitionNode CreateSearchResultType()
        => ObjectType(
            "__SearchResult",
            [
                Field("cursor", "String!"),
                Field("coordinate", "String!"),
                Field("definition", "__SchemaDefinition!"),
                Field("pathsToRoot", "[[String!]!]!"),
                Field("score", "Float")
            ]);

    private static UnionTypeDefinitionNode CreateSchemaDefinitionType()
        => UnionType(
            "__SchemaDefinition",
            "__Type",
            "__Field",
            "__InputValue",
            "__EnumValue",
            "__Directive");

    /// <summary>
    /// Gets the arguments of an introspection field that filters the members it returns.
    /// </summary>
    private static InputValueDefinitionNode[] FilterArguments(bool optIn)
        => [s_includeDeprecated, .. When(optIn, s_includeOptIn)];

    /// <summary>
    /// Gets the specified items when the condition is met, otherwise nothing.
    /// </summary>
    private static T[] When<T>(bool condition, params T[] items)
        => condition ? items : [];

    private static ObjectTypeDefinitionNode ObjectType(
        string name,
        IReadOnlyList<FieldDefinitionNode> fields)
        => new(null, new NameNode(name), null, [], [], fields);

    private static EnumTypeDefinitionNode EnumType(string name, params string[] values)
        => new(
            null,
            new NameNode(name),
            null,
            [],
            [.. values.Select(static value =>
                new EnumValueDefinitionNode(null, new NameNode(value), null, []))]);

    private static UnionTypeDefinitionNode UnionType(string name, params string[] types)
        => new(
            null,
            new NameNode(name),
            null,
            [],
            [.. types.Select(static type => new NamedTypeNode(type))]);

    private static FieldDefinitionNode Field(string name, string type)
        => Field(name, type, []);

    private static FieldDefinitionNode Field(
        string name,
        string type,
        IReadOnlyList<InputValueDefinitionNode> arguments)
        => new(
            null,
            new NameNode(name),
            null,
            arguments,
            Utf8GraphQLParser.Syntax.ParseTypeReference(type),
            []);

    private static InputValueDefinitionNode Argument(
        string name,
        string type,
        IValueNode? defaultValue = null)
        => new(
            null,
            new NameNode(name),
            null,
            Utf8GraphQLParser.Syntax.ParseTypeReference(type),
            defaultValue,
            []);

    /// <summary>
    /// Identifies an introspection schema document. Every schema option that the document
    /// depends on must be part of the shape, as documents are cached and shared by shape.
    /// </summary>
    private readonly record struct Shape(bool OptInFeatures, bool SemanticIntrospection)
    {
        public static Shape From(IFusionSchemaOptions options)
            => new(
                OptInFeatures: options.EnableOptInFeatures,
                SemanticIntrospection: options.EnableSemanticIntrospection);
    }
}
