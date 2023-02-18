using HotChocolate.Language;
using HotChocolate.Utilities;
using static HotChocolate.Skimmed.WellKnownContextData;
using static HotChocolate.WellKnownDirectives;

namespace HotChocolate.Skimmed.Serialization;

public static class SchemaParser
{
    public static Schema Parse(ReadOnlySpan<byte> sourceText)
    {
        var document = Utf8GraphQLParser.Parse(sourceText);
        var schema = new Schema();

        DiscoverDirectives(schema, document);
        DiscoverTypes(schema, document);
        DiscoverExtensions(schema, document);

        BuildTypes(schema, document);
        ExtendTypes(schema, document);

        return schema;
    }

    private static void DiscoverDirectives(Schema schema, DocumentNode document)
    {
        foreach (var definition in document.Definitions)
        {
            if (definition is DirectiveDefinitionNode def)
            {
                if (schema.Directives.ContainsName(def.Name.Value))
                {
                    // TODO : parsing error
                    throw new Exception("duplicate");
                }

                schema.Directives.Add(new DirectiveType(def.Name.Value));
            }
        }
    }

    private static void DiscoverTypes(Schema schema, DocumentNode document)
    {
        foreach (var definition in document.Definitions)
        {
            if (definition is ITypeDefinitionNode typeDef)
            {
                if (schema.Types.ContainsName(typeDef.Name.Value))
                {
                    // TODO : parsing error
                    throw new Exception("duplicate");
                }

                switch (typeDef)
                {
                    case EnumTypeDefinitionNode def:
                        break;

                    case InputObjectTypeDefinitionNode:
                        schema.Types.Add(new InputObjectType(typeDef.Name.Value));
                        break;

                    case InterfaceTypeDefinitionNode:
                        schema.Types.Add(new InterfaceType(typeDef.Name.Value));
                        break;

                    case ObjectTypeDefinitionNode:
                        schema.Types.Add(new ObjectType(typeDef.Name.Value));
                        break;

                    case ScalarTypeDefinitionNode:
                        schema.Types.Add(new ScalarType(typeDef.Name.Value));
                        break;

                    case UnionTypeDefinitionNode def:
                        break;

                    default:
                        // TODO : parsing error
                        throw new ArgumentOutOfRangeException(nameof(definition));
                }
            }
        }
    }

    private static void DiscoverExtensions(Schema schema, DocumentNode document)
    {
        foreach (var definition in document.Definitions)
        {
            if (definition is ITypeExtensionNode typeExt &&
                !schema.Types.ContainsName(typeExt.Name.Value))
            {
                switch (definition)
                {
                    case EnumTypeExtensionNode:
                        break;

                    case InputObjectTypeExtensionNode:
                        var inputObjectType = new InputObjectType(typeExt.Name.Value);
                        inputObjectType.ContextData.Add(TypeExtension, true);
                        schema.Types.Add(inputObjectType);
                        break;

                    case InterfaceTypeExtensionNode:
                        var interfaceType = new InterfaceType(typeExt.Name.Value);
                        interfaceType.ContextData.Add(TypeExtension, true);
                        schema.Types.Add(interfaceType);
                        break;

                    case ObjectTypeExtensionNode:
                        var objectType = new ObjectType(typeExt.Name.Value);
                        objectType.ContextData.Add(TypeExtension, true);
                        schema.Types.Add(objectType);
                        break;

                    case ScalarTypeExtensionNode:
                        var scalarType = new ScalarType(typeExt.Name.Value);
                        scalarType.ContextData.Add(TypeExtension, true);
                        schema.Types.Add(scalarType);
                        break;

                    case UnionTypeExtensionNode:
                        break;

                    default:
                        // TODO : parsing error
                        throw new ArgumentOutOfRangeException(nameof(definition));
                }
            }
        }
    }

    private static void BuildTypes(Schema schema, DocumentNode document)
    {
        foreach (var definition in document.Definitions)
        {
            if (definition is ITypeDefinitionNode)
            {
                switch (definition)
                {
                    case EnumTypeDefinitionNode:
                        break;

                    case InputObjectTypeDefinitionNode typeDef:
                        BuildInputObjectType(
                            schema,
                            (InputObjectType)schema.Types[typeDef.Name.Value],
                            typeDef);
                        break;

                    case InterfaceTypeDefinitionNode typeDef:
                        BuildInterfaceType(
                            schema,
                            (InterfaceType)schema.Types[typeDef.Name.Value],
                            typeDef);
                        break;

                    case ObjectTypeDefinitionNode typeDef:
                        BuildObjectType(
                            schema,
                            (ObjectType)schema.Types[typeDef.Name.Value],
                            typeDef);
                        break;

                    case ScalarTypeDefinitionNode typeDef:
                        BuildScalarType(
                            schema,
                            (ScalarType)schema.Types[typeDef.Name.Value],
                            typeDef);
                        break;

                    case UnionTypeDefinitionNode def:
                        break;

                    default:
                        // TODO : parsing error
                        throw new ArgumentOutOfRangeException(nameof(definition));
                }
            }
        }
    }

    private static void ExtendTypes(Schema schema, DocumentNode document)
    {
        foreach (var definition in document.Definitions)
        {
            if (definition is ITypeExtensionNode)
            {
                switch (definition)
                {
                    case EnumTypeExtensionNode:
                        break;

                    case InputObjectTypeExtensionNode typeDef:
                        ExtendInputObjectType(
                            schema,
                            (InputObjectType)schema.Types[typeDef.Name.Value],
                            typeDef);
                        break;

                    case InterfaceTypeExtensionNode typeDef:
                        ExtendInterfaceType(
                            schema,
                            (InterfaceType)schema.Types[typeDef.Name.Value],
                            typeDef);
                        break;

                    case ObjectTypeExtensionNode typeDef:
                        ExtendObjectType(
                            schema,
                            (ObjectType)schema.Types[typeDef.Name.Value],
                            typeDef);
                        break;

                    case ScalarTypeExtensionNode def:
                        break;

                    case UnionTypeExtensionNode def:
                        break;

                    default:
                        // TODO : parsing error
                        throw new ArgumentOutOfRangeException(nameof(definition));
                }
            }
        }
    }

    private static void BuildObjectType(
        Schema schema,
        ObjectType type,
        ObjectTypeDefinitionNode node)
    {
        type.Description = node.Description?.Value;
        BuildComplexType(schema, type, node);
    }

    private static void ExtendObjectType(
        Schema schema,
        ObjectType type,
        ObjectTypeExtensionNode node)
        => BuildComplexType(schema, type, node);

    private static void BuildInterfaceType(
        Schema schema,
        InterfaceType type,
        InterfaceTypeDefinitionNode node)
    {
        type.Description = node.Description?.Value;
        BuildComplexType(schema, type, node);
    }

    private static void ExtendInterfaceType(
        Schema schema,
        InterfaceType type,
        InterfaceTypeExtensionNode node)
        => BuildComplexType(schema, type, node);

    private static void BuildComplexType(
        Schema schema,
        ComplexType type,
        ComplexTypeDefinitionNodeBase node)
    {
        BuildDirectiveCollection(schema, type.Directives, node.Directives);

        foreach (var interfaceRef in node.Interfaces)
        {
            type.Implements.Add(schema.Types.ResolveType<InterfaceType>(interfaceRef));
        }

        foreach (var fieldNode in node.Fields)
        {
            if (type.Fields.ContainsName(fieldNode.Name.Value))
            {
                // todo : parser error
                throw new Exception("");
            }

            var field = new OutputField(fieldNode.Name.Value);
            field.Description = fieldNode.Description?.Value;
            field.Type = schema.Types.ResolveType(fieldNode.Type);

            BuildDirectiveCollection(schema, field.Directives, fieldNode.Directives);

            if (IsDeprecated(field.Directives, out var reason))
            {
                field.IsDeprecated = true;
                field.DeprecationReason = reason;
            }

            foreach (var argumentNode in fieldNode.Arguments)
            {
                if (field.Arguments.ContainsName(argumentNode.Name.Value))
                {
                    // todo : parser error
                    throw new Exception("");
                }

                var argument = new InputField(argumentNode.Name.Value);
                argument.Description = argumentNode.Description?.Value;
                argument.Type = schema.Types.ResolveType(argumentNode.Type);
                argument.DefaultValue = argumentNode.DefaultValue;

                BuildDirectiveCollection(schema, argument.Directives, argumentNode.Directives);

                if (IsDeprecated(argument.Directives, out reason))
                {
                    argument.IsDeprecated = true;
                    argument.DeprecationReason = reason;
                }

                field.Arguments.Add(argument);
            }

            type.Fields.Add(field);
        }
    }

    private static void BuildInputObjectType(
        Schema schema,
        InputObjectType type,
        InputObjectTypeDefinitionNode node)
    {
        type.Description = node.Description?.Value;
        ExtendInputObjectType(schema, type, node);
    }

    private static void ExtendInputObjectType(
        Schema schema,
        InputObjectType type,
        InputObjectTypeDefinitionNodeBase node)
    {
        BuildDirectiveCollection(schema, type.Directives, node.Directives);

        foreach (var fieldNode in node.Fields)
        {
            if (type.Fields.ContainsName(fieldNode.Name.Value))
            {
                // todo : parser error
                throw new Exception("");
            }

            var field = new InputField(fieldNode.Name.Value);
            field.Description = fieldNode.Description?.Value;
            field.Type = schema.Types.ResolveType(fieldNode.Type);

            BuildDirectiveCollection(schema, field.Directives, fieldNode.Directives);

            if (IsDeprecated(field.Directives, out var reason))
            {
                field.IsDeprecated = true;
                field.DeprecationReason = reason;
            }

            type.Fields.Add(field);
        }
    }

    private static void BuildScalarType(
        Schema schema,
        ScalarType type,
        ScalarTypeDefinitionNode node)
    {

    }

    private static void BuildDirectiveCollection(
        Schema schema,
        DirectiveCollection directives,
        IReadOnlyList<DirectiveNode> nodes) { }

    private static bool IsDeprecated(DirectiveCollection directives, out string? reason)
    {
        reason = null;

        var deprecated = directives.FirstOrDefault(Deprecated);
        if (deprecated is not null)
        {
            var reasonArg = deprecated.Arguments.FirstOrDefault(
                t => t.Name.EqualsOrdinal(DeprecationReasonArgument));

            if (reasonArg?.Value is StringValueNode reasonVal)
            {
                reason = reasonVal.Value;
            }

            return true;
        }

        return false;
    }
}

static file class SchemaParserExtensions
{
    public static T ResolveType<T>(this TypeCollection types, ITypeNode typeRef)
        where T : IType
        => (T)ResolveType(types, typeRef);

    public static IType ResolveType(this TypeCollection types, ITypeNode typeRef)
    {
        switch (typeRef)
        {
            case NonNullTypeNode nonNullTypeRef:
                return new NonNullType(ResolveType(types, nonNullTypeRef.Type));

            case ListTypeNode listTypeRef:
                return new ListType(ResolveType(types, listTypeRef.Type));

            case NamedTypeNode namedTypeRef:
                if (types.TryGetType(namedTypeRef.Name.Value, out var type))
                {
                    return type;
                }

                var missing = new MissingType(namedTypeRef.Name.Value);
                types.Add(missing);
                return missing;

            default:
                // TODO : parsing error
                throw new ArgumentOutOfRangeException(nameof(typeRef));
        }
    }
}
