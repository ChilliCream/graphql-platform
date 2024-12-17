using System.Text;
using HotChocolate.Language;

namespace HotChocolate.Skimmed.Serialization;

public static class SchemaParser
{
    public static SchemaDefinition Parse(string sourceText)
        => Parse(Encoding.UTF8.GetBytes(sourceText));

    public static SchemaDefinition Parse(ReadOnlySpan<byte> sourceText)
    {
        var schema = new SchemaDefinition();
        Parse(schema, sourceText);
        return schema;
    }

    public static void Parse(SchemaDefinition schema, string sourceText)
        => Parse(schema, Encoding.UTF8.GetBytes(sourceText));

    public static void Parse(SchemaDefinition schema, ReadOnlySpan<byte> sourceText)
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

    private static void DiscoverDirectives(SchemaDefinition schema, DocumentNode document)
    {
        foreach (var definition in document.Definitions)
        {
            if (definition is DirectiveDefinitionNode def)
            {
                if (schema.DirectiveDefinitions.ContainsName(def.Name.Value))
                {
                    // TODO : parsing error
                    throw new Exception("duplicate");
                }

                schema.DirectiveDefinitions.Add(
                    new DirectiveDefinition(def.Name.Value)
                    {
                        IsSpecDirective = BuiltIns.IsBuiltInDirective(def.Name.Value)
                    });
            }
        }
    }

    private static void DiscoverTypes(SchemaDefinition schema, DocumentNode document)
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
                    case EnumTypeDefinitionNode:
                        schema.Types.Add(new EnumTypeDefinition(typeDef.Name.Value));
                        break;

                    case InputObjectTypeDefinitionNode:
                        schema.Types.Add(new InputObjectTypeDefinition(typeDef.Name.Value));
                        break;

                    case InterfaceTypeDefinitionNode:
                        schema.Types.Add(new InterfaceTypeDefinition(typeDef.Name.Value));
                        break;

                    case ObjectTypeDefinitionNode:
                        schema.Types.Add(new ObjectTypeDefinition(typeDef.Name.Value));
                        break;

                    case ScalarTypeDefinitionNode:
                        schema.Types.Add(
                            new ScalarTypeDefinition(typeDef.Name.Value)
                            {
                                IsSpecScalar = BuiltIns.IsBuiltInScalar(typeDef.Name.Value)
                            });
                        break;

                    case UnionTypeDefinitionNode:
                        schema.Types.Add(new UnionTypeDefinition(typeDef.Name.Value));
                        break;

                    default:
                        // TODO : parsing error
                        throw new ArgumentOutOfRangeException(nameof(definition));
                }
            }
        }
    }

    private static void DiscoverExtensions(SchemaDefinition schema, DocumentNode document)
    {
        foreach (var definition in document.Definitions)
        {
            if (definition is ITypeExtensionNode typeExt &&
                !schema.Types.ContainsName(typeExt.Name.Value))
            {
                switch (definition)
                {
                    case EnumTypeExtensionNode:
                        var enumType = new EnumTypeDefinition(typeExt.Name.Value);
                        enumType.GetTypeMetadata().IsExtension = true;
                        schema.Types.Add(enumType);
                        break;

                    case InputObjectTypeExtensionNode:
                        var inputObjectType = new InputObjectTypeDefinition(typeExt.Name.Value);
                        inputObjectType.GetTypeMetadata().IsExtension = true;
                        schema.Types.Add(inputObjectType);
                        break;

                    case InterfaceTypeExtensionNode:
                        var interfaceType = new InterfaceTypeDefinition(typeExt.Name.Value);
                        interfaceType.GetTypeMetadata().IsExtension = true;
                        schema.Types.Add(interfaceType);
                        break;

                    case ObjectTypeExtensionNode:
                        var objectType = new ObjectTypeDefinition(typeExt.Name.Value);
                        objectType.GetTypeMetadata().IsExtension = true;
                        schema.Types.Add(objectType);
                        break;

                    case ScalarTypeExtensionNode:
                        var scalarType = new ScalarTypeDefinition(typeExt.Name.Value);
                        scalarType.GetTypeMetadata().IsExtension = true;
                        schema.Types.Add(scalarType);
                        break;

                    case UnionTypeExtensionNode:
                        var unionType = new UnionTypeDefinition(typeExt.Name.Value);
                        unionType.GetTypeMetadata().IsExtension = true;
                        schema.Types.Add(unionType);
                        break;

                    default:
                        // TODO : parsing error
                        throw new ArgumentOutOfRangeException(nameof(definition));
                }
            }
        }
    }

    private static void BuildTypes(SchemaDefinition schema, DocumentNode document)
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
                            (EnumTypeDefinition)schema.Types[typeDef.Name.Value],
                            typeDef);
                        break;

                    case InputObjectTypeDefinitionNode typeDef:
                        BuildInputObjectType(
                            schema,
                            (InputObjectTypeDefinition)schema.Types[typeDef.Name.Value],
                            typeDef);
                        break;

                    case InterfaceTypeDefinitionNode typeDef:
                        BuildInterfaceType(
                            schema,
                            (InterfaceTypeDefinition)schema.Types[typeDef.Name.Value],
                            typeDef);
                        break;

                    case ObjectTypeDefinitionNode typeDef:
                        BuildObjectType(
                            schema,
                            (ObjectTypeDefinition)schema.Types[typeDef.Name.Value],
                            typeDef);
                        break;

                    case ScalarTypeDefinitionNode typeDef:
                        BuildScalarType(
                            schema,
                            (ScalarTypeDefinition)schema.Types[typeDef.Name.Value],
                            typeDef);
                        break;

                    case UnionTypeDefinitionNode typeDef:
                        BuildUnionType(
                            schema,
                            (UnionTypeDefinition)schema.Types[typeDef.Name.Value],
                            typeDef);
                        break;

                    default:
                        // TODO : parsing error
                        throw new ArgumentOutOfRangeException(nameof(definition));
                }
            }
        }
    }

    private static void ExtendTypes(SchemaDefinition schema, DocumentNode document)
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
                            (EnumTypeDefinition)schema.Types[typeDef.Name.Value],
                            typeDef);
                        break;

                    case InputObjectTypeExtensionNode typeDef:
                        ExtendInputObjectType(
                            schema,
                            (InputObjectTypeDefinition)schema.Types[typeDef.Name.Value],
                            typeDef);
                        break;

                    case InterfaceTypeExtensionNode typeDef:
                        ExtendInterfaceType(
                            schema,
                            (InterfaceTypeDefinition)schema.Types[typeDef.Name.Value],
                            typeDef);
                        break;

                    case ObjectTypeExtensionNode typeDef:
                        ExtendObjectType(
                            schema,
                            (ObjectTypeDefinition)schema.Types[typeDef.Name.Value],
                            typeDef);
                        break;

                    case ScalarTypeExtensionNode typeDef:
                        ExtendScalarType(
                            schema,
                            (ScalarTypeDefinition)schema.Types[typeDef.Name.Value],
                            typeDef);
                        break;

                    case UnionTypeExtensionNode typeDef:
                        ExtendUnionType(
                            schema,
                            (UnionTypeDefinition)schema.Types[typeDef.Name.Value],
                            typeDef);
                        break;

                    default:
                        // TODO : parsing error
                        throw new ArgumentOutOfRangeException(nameof(definition));
                }
            }
        }
    }

    private static void BuildAndExtendSchema(SchemaDefinition schema, DocumentNode document)
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
                schema.Types.TryGetType<ObjectTypeDefinition>("Query", out var queryType))
            {
                schema.QueryType = queryType;
            }

            if (schema.MutationType is null &&
                schema.Types.TryGetType<ObjectTypeDefinition>("Mutation", out var mutationType))
            {
                schema.MutationType = mutationType;
            }

            if (schema.SubscriptionType is null &&
                schema.Types.TryGetType<ObjectTypeDefinition>("Subscription", out var subscriptionType))
            {
                schema.SubscriptionType = subscriptionType;
            }
        }
    }

    private static void BuildObjectType(
        SchemaDefinition schema,
        ObjectTypeDefinition type,
        ObjectTypeDefinitionNode node)
    {
        type.Description = node.Description?.Value;
        BuildComplexType(schema, type, node);
    }

    private static void ExtendObjectType(
        SchemaDefinition schema,
        ObjectTypeDefinition type,
        ObjectTypeExtensionNode node)
        => BuildComplexType(schema, type, node);

    private static void BuildInterfaceType(
        SchemaDefinition schema,
        InterfaceTypeDefinition type,
        InterfaceTypeDefinitionNode node)
    {
        type.Description = node.Description?.Value;
        BuildComplexType(schema, type, node);
    }

    private static void ExtendInterfaceType(
        SchemaDefinition schema,
        InterfaceTypeDefinition type,
        InterfaceTypeExtensionNode node)
        => BuildComplexType(schema, type, node);

    private static void BuildComplexType(
        SchemaDefinition schema,
        ComplexTypeDefinition type,
        ComplexTypeDefinitionNodeBase node)
    {
        BuildDirectiveCollection(schema, type.Directives, node.Directives);

        foreach (var interfaceRef in node.Interfaces)
        {
            type.Implements.Add(schema.Types.ResolveType<InterfaceTypeDefinition>(interfaceRef));
        }

        foreach (var fieldNode in node.Fields)
        {
            if (type.Fields.ContainsName(fieldNode.Name.Value))
            {
                // todo : parser error
                throw new Exception("");
            }

            var field = new OutputFieldDefinition(fieldNode.Name.Value);
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

                var argument = new InputFieldDefinition(argumentNode.Name.Value);
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
        SchemaDefinition schema,
        InputObjectTypeDefinition type,
        InputObjectTypeDefinitionNode node)
    {
        type.Description = node.Description?.Value;
        ExtendInputObjectType(schema, type, node);
    }

    private static void ExtendInputObjectType(
        SchemaDefinition schema,
        InputObjectTypeDefinition type,
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

            var field = new InputFieldDefinition(fieldNode.Name.Value);
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
        SchemaDefinition schema,
        EnumTypeDefinition type,
        EnumTypeDefinitionNode node)
    {
        type.Description = node.Description?.Value;
        ExtendEnumType(schema, type, node);
    }

    private static void ExtendEnumType(
        SchemaDefinition schema,
        EnumTypeDefinition type,
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
        SchemaDefinition schema,
        UnionTypeDefinition type,
        UnionTypeDefinitionNode node)
    {
        type.Description = node.Description?.Value;
        ExtendUnionType(schema, type, node);
    }

    private static void ExtendUnionType(
        SchemaDefinition schema,
        UnionTypeDefinition type,
        UnionTypeDefinitionNodeBase node)
    {
        BuildDirectiveCollection(schema, type.Directives, node.Directives);

        foreach (var objectTypeRef in node.Types)
        {
            var objectType = (ObjectTypeDefinition)schema.Types[objectTypeRef.Name.Value];

            if (type.Types.Contains(objectType))
            {
                continue;
            }

            type.Types.Add(objectType);
        }
    }

    private static void BuildScalarType(
        SchemaDefinition schema,
        ScalarTypeDefinition type,
        ScalarTypeDefinitionNode node)
    {
        type.Description = node.Description?.Value;
        BuildDirectiveCollection(schema, type.Directives, node.Directives);
    }

    private static void ExtendScalarType(
        SchemaDefinition schema,
        ScalarTypeDefinition type,
        ScalarTypeExtensionNode node)
    {
        BuildDirectiveCollection(schema, type.Directives, node.Directives);
    }

    private static void BuildDirectiveTypes(SchemaDefinition schema, DocumentNode document)
    {
        foreach (var definition in document.Definitions)
        {
            if (definition is DirectiveDefinitionNode directiveDef)
            {
                BuildDirectiveType(
                    schema,
                    schema.DirectiveDefinitions[directiveDef.Name.Value],
                    directiveDef);
            }
        }
    }

    private static void BuildDirectiveType(
        SchemaDefinition schema,
        DirectiveDefinition type,
        DirectiveDefinitionNode node)
    {
        type.Description = node.Description?.Value;
        type.IsRepeatable = node.IsRepeatable;

        foreach (var argumentNode in node.Arguments)
        {
            var argument = new InputFieldDefinition(argumentNode.Name.Value);
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
        SchemaDefinition schema,
        SchemaDefinitionNode node)
    {
        schema.Description = node.Description?.Value;
        ExtendSchema(schema, node);
    }

    private static void ExtendSchema(
        SchemaDefinition schema,
        SchemaDefinitionNodeBase node)
    {
        BuildDirectiveCollection(schema, schema.Directives, node.Directives);

        foreach (var operationType in node.OperationTypes)
        {
            var typeName = operationType.Type.Name.Value;

            switch (operationType.Operation)
            {
                case OperationType.Query:
                    schema.QueryType = (ObjectTypeDefinition)schema.Types[typeName];
                    break;

                case OperationType.Mutation:
                    schema.MutationType = (ObjectTypeDefinition)schema.Types[typeName];
                    break;

                case OperationType.Subscription:
                    schema.SubscriptionType = (ObjectTypeDefinition)schema.Types[typeName];
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private static void BuildDirectiveCollection(
        SchemaDefinition schema,
        IDirectiveCollection directives,
        IReadOnlyList<DirectiveNode> nodes)
    {
        foreach (var directiveNode in nodes)
        {
            if (!schema.DirectiveDefinitions.TryGetDirective(
                directiveNode.Name.Value,
                out var directiveType))
            {
                if (directiveNode.Name.Value == BuiltIns.Deprecated.Name)
                {
                    directiveType = BuiltIns.Deprecated.Create(schema);
                }
                else if (directiveNode.Name.Value == BuiltIns.SpecifiedBy.Name)
                {
                    directiveType = BuiltIns.SpecifiedBy.Create(schema);
                }
                else if (directiveNode.Name.Value == BuiltIns.SemanticNonNull.Name)
                {
                    directiveType = BuiltIns.SemanticNonNull.Create(schema);
                }
                else
                {
                    directiveType = new DirectiveDefinition(directiveNode.Name.Value);
                    // TODO: This is problematic, but currently necessary for the Fusion
                    // directives to work, since they don't have definitions in the source schema.
                    directiveType.IsRepeatable = true;
                }

                schema.DirectiveDefinitions.Add(directiveType);
            }

            var directive = new Directive(
                directiveType,
                directiveNode.Arguments.Select(t => new ArgumentAssignment(t.Name.Value, t.Value)).ToList());
            directives.Add(directive);
        }
    }

    private static bool IsDeprecated(IDirectiveCollection directives, out string? reason)
    {
        reason = null;

        var deprecated = directives.FirstOrDefault(BuiltIns.Deprecated.Name);

        if (deprecated is not null)
        {
            var reasonArg = deprecated.Arguments.FirstOrDefault(
                t => t.Name.Equals(BuiltIns.Deprecated.Reason, StringComparison.Ordinal));

            if (reasonArg?.Value is StringValueNode reasonVal)
            {
                reason = reasonVal.Value;
            }

            return true;
        }

        return false;
    }
}

file static class SchemaParserExtensions
{
    public static T ResolveType<T>(this ITypeDefinitionCollection typesDefinition, ITypeNode typeRef)
        where T : ITypeDefinition
        => (T)ResolveType(typesDefinition, typeRef);

    public static ITypeDefinition ResolveType(this ITypeDefinitionCollection typesDefinition, ITypeNode typeRef)
    {
        switch (typeRef)
        {
            case NonNullTypeNode nonNullTypeRef:
                return new NonNullTypeDefinition(ResolveType(typesDefinition, nonNullTypeRef.Type));

            case ListTypeNode listTypeRef:
                return new ListTypeDefinition(ResolveType(typesDefinition, listTypeRef.Type));

            case NamedTypeNode namedTypeRef:
                if (typesDefinition.TryGetType(namedTypeRef.Name.Value, out var type))
                {
                    return type;
                }

                if (BuiltIns.IsBuiltInScalar(namedTypeRef.Name.Value))
                {
                    var scalar = new ScalarTypeDefinition(namedTypeRef.Name.Value) { IsSpecScalar = true, };
                    typesDefinition.Add(scalar);
                    return scalar;
                }

                var missing = new MissingTypeDefinition(namedTypeRef.Name.Value);
                typesDefinition.Add(missing);
                return missing;

            default:
                // TODO : parsing error
                throw new ArgumentOutOfRangeException(nameof(typeRef));
        }
    }
}
