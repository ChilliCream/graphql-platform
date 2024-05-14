using System.Text;
using HotChocolate.Language;
using HotChocolate.Utilities;
using static HotChocolate.Skimmed.WellKnownContextData;
using static HotChocolate.WellKnownDirectives;

namespace HotChocolate.Skimmed.Serialization;

public static class SchemaParser
{
    public static Schema Parse(string sourceText)
        => Parse(Encoding.UTF8.GetBytes(sourceText));

    public static Schema Parse(ReadOnlySpan<byte> sourceText)
    {
        var schema = new Schema();
        Parse(schema, sourceText);
        return schema;
    }

    public static void Parse(Schema schema, string sourceText)
        => Parse(schema, Encoding.UTF8.GetBytes(sourceText));

    public static void Parse(Schema schema, ReadOnlySpan<byte> sourceText)
    {
        var document = Utf8GraphQLParser.Parse(sourceText);

        DiscoverDirectives(schema, document);
        DiscoverTypes(schema, document);
        DiscoverExtensions(schema, document);

        BuildTypes(schema, document);
        ExtendTypes(schema, document);
        BuildDirectiveTypes(schema, document);
        BuildAndExtendSchema(schema, document);
    }

    private static void DiscoverDirectives(Schema schema, DocumentNode document)
    {
        foreach (var definition in document.Definitions)
        {
            if (definition is DirectiveDefinitionNode def)
            {
                if (schema.DirectiveTypes.ContainsName(def.Name.Value))
                {
                    // TODO : parsing error
                    throw new Exception("duplicate");
                }

                schema.DirectiveTypes.Add(new DirectiveType(def.Name.Value));
            }
        }
    }

    private static void DiscoverTypes(Schema schema, DocumentNode document)
    {
        foreach (var definition in document.Definitions)
        {
            if (definition is ITypeDefinitionNode typeDef)
            {
                if (SpecScalarTypes.IsSpecScalar(typeDef.Name.Value))
                {
                    continue;
                }

                if (schema.Types.ContainsName(typeDef.Name.Value))
                {
                    // TODO : parsing error
                    throw new Exception("duplicate");
                }

                switch (typeDef)
                {
                    case EnumTypeDefinitionNode:
                        schema.Types.Add(new EnumType(typeDef.Name.Value));
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

                    case UnionTypeDefinitionNode:
                        schema.Types.Add(new UnionType(typeDef.Name.Value));
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
                        var enumType = new EnumType(typeExt.Name.Value);
                        enumType.ContextData.Add(TypeExtension, true);
                        schema.Types.Add(enumType);
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
                        var unionType = new UnionType(typeExt.Name.Value);
                        unionType.ContextData.Add(TypeExtension, true);
                        schema.Types.Add(unionType);
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
                    case EnumTypeDefinitionNode typeDef:
                        BuildEnumType(
                            schema,
                            (EnumType)schema.Types[typeDef.Name.Value],
                            typeDef);
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
                        if (SpecScalarTypes.IsSpecScalar(typeDef.Name.Value))
                        {
                            continue;
                        }

                        BuildScalarType(
                            schema,
                            (ScalarType)schema.Types[typeDef.Name.Value],
                            typeDef);
                        break;

                    case UnionTypeDefinitionNode typeDef:
                        BuildUnionType(
                            schema,
                            (UnionType)schema.Types[typeDef.Name.Value],
                            typeDef);
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
                    case EnumTypeExtensionNode typeDef:
                        ExtendEnumType(
                            schema,
                            (EnumType)schema.Types[typeDef.Name.Value],
                            typeDef);
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

                    case ScalarTypeExtensionNode typeDef:
                        ExtendScalarType(
                            schema,
                            (ScalarType)schema.Types[typeDef.Name.Value],
                            typeDef);
                        break;

                    case UnionTypeExtensionNode typeDef:
                        ExtendUnionType(
                            schema,
                            (UnionType)schema.Types[typeDef.Name.Value],
                            typeDef);
                        break;

                    default:
                        // TODO : parsing error
                        throw new ArgumentOutOfRangeException(nameof(definition));
                }
            }
        }
    }

    private static void BuildAndExtendSchema(Schema schema, DocumentNode document)
    {
        var hasDefinition = false;
        
        foreach (var definition in document.Definitions)
        {
            if (definition is SchemaDefinitionNode node)
            {
                BuildSchema(schema, node);
                hasDefinition = true;
                break;
            }
        }

        foreach (var definition in document.Definitions)
        {
            if (definition is SchemaExtensionNode node)
            {
                ExtendSchema(schema, node);
            }
        }

        // if we did not find a schema definition we will infer the root types.
        if (!hasDefinition)
        {
            if (schema.QueryType is null && 
                schema.Types.TryGetType<ObjectType>("Query", out var queryType))
            {
                schema.QueryType = queryType;
            }
            
            if (schema.MutationType is null && 
                schema.Types.TryGetType<ObjectType>("Mutation", out var mutationType))
            {
                schema.MutationType = mutationType;
            }
            
            if (schema.SubscriptionType is null && 
                schema.Types.TryGetType<ObjectType>("Subscription", out var subscriptionType))
            {
                schema.SubscriptionType = subscriptionType;
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

    private static void BuildEnumType(
        Schema schema,
        EnumType type,
        EnumTypeDefinitionNode node)
    {
        type.Description = node.Description?.Value;
        ExtendEnumType(schema, type, node);
    }

    private static void ExtendEnumType(
        Schema schema,
        EnumType type,
        EnumTypeDefinitionNodeBase node)
    {
        BuildDirectiveCollection(schema, type.Directives, node.Directives);

        foreach (var enumValue in node.Values)
        {
            if (type.Values.ContainsName(enumValue.Name.Value))
            {
                continue;
            }

            var value = new EnumValue(enumValue.Name.Value);
            value.Description = enumValue.Description?.Value;

            BuildDirectiveCollection(schema, value.Directives, enumValue.Directives);

            if (IsDeprecated(value.Directives, out var reason))
            {
                value.IsDeprecated = true;
                value.DeprecationReason = reason;
            }

            type.Values.Add(value);
        }
    }

    private static void BuildUnionType(
        Schema schema,
        UnionType type,
        UnionTypeDefinitionNode node)
    {
        type.Description = node.Description?.Value;
        ExtendUnionType(schema, type, node);
    }

    private static void ExtendUnionType(
        Schema schema,
        UnionType type,
        UnionTypeDefinitionNodeBase node)
    {
        BuildDirectiveCollection(schema, type.Directives, node.Directives);

        foreach (var objectTypeRef in node.Types)
        {
            var objectType = (ObjectType)schema.Types[objectTypeRef.Name.Value];

            if (type.Types.Contains(objectType))
            {
                continue;
            }

            type.Types.Add(objectType);
        }
    }

    private static void BuildScalarType(
        Schema schema,
        ScalarType type,
        ScalarTypeDefinitionNode node)
    {
        type.Description = node.Description?.Value;
        BuildDirectiveCollection(schema, type.Directives, node.Directives);
    }

    private static void ExtendScalarType(
        Schema schema,
        ScalarType type,
        ScalarTypeExtensionNode node)
    {
        BuildDirectiveCollection(schema, type.Directives, node.Directives);
    }

    private static void BuildDirectiveTypes(Schema schema, DocumentNode document)
    {
        foreach (var definition in document.Definitions)
        {
            if (definition is DirectiveDefinitionNode directiveDef)
            {
                BuildDirectiveType(
                    schema,
                    schema.DirectiveTypes[directiveDef.Name.Value],
                    directiveDef);
            }
        }
    }

    private static void BuildDirectiveType(
        Schema schema,
        DirectiveType type,
        DirectiveDefinitionNode node)
    {
        type.Description = node.Description?.Value;
        type.IsRepeatable = node.IsRepeatable;

        foreach (var argumentNode in node.Arguments)
        {
            var argument = new InputField(argumentNode.Name.Value);
            argument.Description = argumentNode.Description?.Value;
            argument.Type = schema.Types.ResolveType(argumentNode.Type);
            argument.DefaultValue = argumentNode.DefaultValue;

            BuildDirectiveCollection(schema, argument.Directives, argumentNode.Directives);

            if (IsDeprecated(argument.Directives, out var reason))
            {
                argument.IsDeprecated = true;
                argument.DeprecationReason = reason;
            }

            type.Arguments.Add(argument);
        }

        foreach (var locationNode in node.Locations)
        {
            if (!Language.DirectiveLocation.TryParse(locationNode.Value, out var parsedLocation))
            {
                throw new Exception("");
            }

            type.Locations |= parsedLocation.MapLocation();
        }
    }

    private static void BuildSchema(
        Schema schema,
        SchemaDefinitionNode node)
    {
        schema.Description = node.Description?.Value;
        ExtendSchema(schema, node);
    }

    private static void ExtendSchema(
        Schema schema,
        SchemaDefinitionNodeBase node)
    {
        BuildDirectiveCollection(schema, schema.Directives, node.Directives);

        foreach (var operationType in node.OperationTypes)
        {
            var typeName = operationType.Type.Name.Value;

            switch (operationType.Operation)
            {
                case OperationType.Query:
                    schema.QueryType = (ObjectType)schema.Types[typeName];
                    break;

                case OperationType.Mutation:
                    schema.MutationType = (ObjectType)schema.Types[typeName];
                    break;

                case OperationType.Subscription:
                    schema.SubscriptionType = (ObjectType)schema.Types[typeName];
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private static void BuildDirectiveCollection(
        Schema schema,
        DirectiveCollection directives,
        IReadOnlyList<DirectiveNode> nodes)
    {
        foreach (var directiveNode in nodes)
        {
            if (!schema.DirectiveTypes.TryGetDirective(
                directiveNode.Name.Value,
                out var directiveType))
            {
                directiveType = new DirectiveType(directiveNode.Name.Value);
                directiveType.IsRepeatable = true;
                schema.DirectiveTypes.Add(directiveType);
            }

            var directive = new Directive(
                directiveType,
                directiveNode.Arguments.Select(t => new Argument(t.Name.Value, t.Value)).ToList());
            directives.Add(directive);
        }
    }

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

                if (SpecScalarTypes.IsSpecScalar(namedTypeRef.Name.Value))
                {
                    var scalar = new ScalarType(namedTypeRef.Name.Value) { IsSpecScalar = true, };
                    types.Add(scalar);
                    return scalar;
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
