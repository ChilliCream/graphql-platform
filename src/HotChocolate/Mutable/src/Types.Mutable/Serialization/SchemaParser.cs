using System.Text;
using HotChocolate.Language;

namespace HotChocolate.Types.Mutable.Serialization;

public static class SchemaParser
{
    public static MutableSchemaDefinition Parse(string sourceText)
        => Parse(Encoding.UTF8.GetBytes(sourceText));

    public static MutableSchemaDefinition Parse(ReadOnlySpan<byte> sourceText)
    {
        var schema = new MutableSchemaDefinition();
        Parse(schema, sourceText);
        return schema;
    }

    public static void Parse(MutableSchemaDefinition schema, string sourceText)
        => Parse(schema, Encoding.UTF8.GetBytes(sourceText));

    public static void Parse(MutableSchemaDefinition schema, ReadOnlySpan<byte> sourceText)
    {
        var document = Utf8GraphQLParser.Parse(sourceText);

        DiscoverTypesAndDirectives(schema, document);
        DiscoverExtensions(schema, document);

        BuildTypes(schema, document);
        ExtendTypes(schema, document);
        BuildDirectiveTypes(schema, document);
        BuildAndExtendSchema(schema, document);
    }

    private static void DiscoverTypesAndDirectives(MutableSchemaDefinition schema, DocumentNode document)
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
                        schema.Types.Add(new MutableEnumTypeDefinition(typeDef.Name.Value));
                        break;

                    case InputObjectTypeDefinitionNode:
                        schema.Types.Add(new MutableInputObjectTypeDefinition(typeDef.Name.Value));
                        break;

                    case InterfaceTypeDefinitionNode:
                        schema.Types.Add(new MutableInterfaceTypeDefinition(typeDef.Name.Value));
                        break;

                    case ObjectTypeDefinitionNode:
                        schema.Types.Add(new MutableObjectTypeDefinition(typeDef.Name.Value));
                        break;

                    case ScalarTypeDefinitionNode:
                        schema.Types.Add(
                            new MutableScalarTypeDefinition(typeDef.Name.Value)
                            {
                                IsSpecScalar = BuiltIns.IsBuiltInScalar(typeDef.Name.Value)
                            });
                        break;

                    case UnionTypeDefinitionNode:
                        schema.Types.Add(new MutableUnionTypeDefinition(typeDef.Name.Value));
                        break;

                    default:
                        // TODO : parsing error
                        throw new ArgumentOutOfRangeException(nameof(definition));
                }
            }

            if (definition is DirectiveDefinitionNode directiveDef)
            {
                if (schema.DirectiveDefinitions.ContainsName(directiveDef.Name.Value))
                {
                    // TODO : parsing error
                    throw new Exception("duplicate");
                }

                schema.DirectiveDefinitions.Add(
                    new MutableDirectiveDefinition(directiveDef.Name.Value)
                    {
                        IsSpecDirective = BuiltIns.IsBuiltInDirective(directiveDef.Name.Value)
                    });
            }
        }
    }

    private static void DiscoverExtensions(MutableSchemaDefinition schema, DocumentNode document)
    {
        foreach (var definition in document.Definitions)
        {
            if (definition is ITypeExtensionNode typeExt
                && !schema.Types.ContainsName(typeExt.Name.Value))
            {
                switch (definition)
                {
                    case EnumTypeExtensionNode:
                        var enumType = new MutableEnumTypeDefinition(typeExt.Name.Value);
                        enumType.GetTypeMetadata().IsExtension = true;
                        schema.Types.Add(enumType);
                        break;

                    case InputObjectTypeExtensionNode:
                        var inputObjectType = new MutableInputObjectTypeDefinition(typeExt.Name.Value);
                        inputObjectType.GetTypeMetadata().IsExtension = true;
                        schema.Types.Add(inputObjectType);
                        break;

                    case InterfaceTypeExtensionNode:
                        var interfaceType = new MutableInterfaceTypeDefinition(typeExt.Name.Value);
                        interfaceType.GetTypeMetadata().IsExtension = true;
                        schema.Types.Add(interfaceType);
                        break;

                    case ObjectTypeExtensionNode:
                        var objectType = new MutableObjectTypeDefinition(typeExt.Name.Value);
                        objectType.GetTypeMetadata().IsExtension = true;
                        schema.Types.Add(objectType);
                        break;

                    case ScalarTypeExtensionNode:
                        var scalarType = new MutableScalarTypeDefinition(typeExt.Name.Value);
                        scalarType.GetTypeMetadata().IsExtension = true;
                        schema.Types.Add(scalarType);
                        break;

                    case UnionTypeExtensionNode:
                        var unionType = new MutableUnionTypeDefinition(typeExt.Name.Value);
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

    private static void BuildTypes(MutableSchemaDefinition schema, DocumentNode document)
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
                            (MutableEnumTypeDefinition)schema.Types[typeDef.Name.Value],
                            typeDef);
                        break;

                    case InputObjectTypeDefinitionNode typeDef:
                        BuildInputObjectType(
                            schema,
                            (MutableInputObjectTypeDefinition)schema.Types[typeDef.Name.Value],
                            typeDef);
                        break;

                    case InterfaceTypeDefinitionNode typeDef:
                        BuildInterfaceType(
                            schema,
                            (MutableInterfaceTypeDefinition)schema.Types[typeDef.Name.Value],
                            typeDef);
                        break;

                    case ObjectTypeDefinitionNode typeDef:
                        BuildObjectType(
                            schema,
                            (MutableObjectTypeDefinition)schema.Types[typeDef.Name.Value],
                            typeDef);
                        break;

                    case ScalarTypeDefinitionNode typeDef:
                        BuildScalarType(
                            schema,
                            (MutableScalarTypeDefinition)schema.Types[typeDef.Name.Value],
                            typeDef);
                        break;

                    case UnionTypeDefinitionNode typeDef:
                        BuildUnionType(
                            schema,
                            (MutableUnionTypeDefinition)schema.Types[typeDef.Name.Value],
                            typeDef);
                        break;

                    default:
                        // TODO : parsing error
                        throw new ArgumentOutOfRangeException(nameof(definition));
                }
            }
        }
    }

    private static void ExtendTypes(MutableSchemaDefinition schema, DocumentNode document)
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
                            (MutableEnumTypeDefinition)schema.Types[typeDef.Name.Value],
                            typeDef);
                        break;

                    case InputObjectTypeExtensionNode typeDef:
                        ExtendInputObjectType(
                            schema,
                            (MutableInputObjectTypeDefinition)schema.Types[typeDef.Name.Value],
                            typeDef);
                        break;

                    case InterfaceTypeExtensionNode typeDef:
                        ExtendInterfaceType(
                            schema,
                            (MutableInterfaceTypeDefinition)schema.Types[typeDef.Name.Value],
                            typeDef);
                        break;

                    case ObjectTypeExtensionNode typeDef:
                        ExtendObjectType(
                            schema,
                            (MutableObjectTypeDefinition)schema.Types[typeDef.Name.Value],
                            typeDef);
                        break;

                    case ScalarTypeExtensionNode typeDef:
                        ExtendScalarType(
                            schema,
                            (MutableScalarTypeDefinition)schema.Types[typeDef.Name.Value],
                            typeDef);
                        break;

                    case UnionTypeExtensionNode typeDef:
                        ExtendUnionType(
                            schema,
                            (MutableUnionTypeDefinition)schema.Types[typeDef.Name.Value],
                            typeDef);
                        break;

                    default:
                        // TODO : parsing error
                        throw new ArgumentOutOfRangeException(nameof(definition));
                }
            }
        }
    }

    private static void BuildAndExtendSchema(MutableSchemaDefinition schema, DocumentNode document)
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
            if (schema.QueryType is null
                && schema.Types.TryGetType<MutableObjectTypeDefinition>("Query", out var queryType))
            {
                schema.QueryType = queryType;
            }

            if (schema.MutationType is null
                && schema.Types.TryGetType<MutableObjectTypeDefinition>("Mutation", out var mutationType))
            {
                schema.MutationType = mutationType;
            }

            if (schema.SubscriptionType is null
                && schema.Types.TryGetType<MutableObjectTypeDefinition>("Subscription", out var subscriptionType))
            {
                schema.SubscriptionType = subscriptionType;
            }
        }
    }

    private static void BuildObjectType(
        MutableSchemaDefinition schema,
        MutableObjectTypeDefinition type,
        ObjectTypeDefinitionNode node)
    {
        type.Description = node.Description?.Value;
        BuildComplexType(schema, type, node);
    }

    private static void ExtendObjectType(
        MutableSchemaDefinition schema,
        MutableObjectTypeDefinition type,
        ObjectTypeExtensionNode node)
        => BuildComplexType(schema, type, node);

    private static void BuildInterfaceType(
        MutableSchemaDefinition schema,
        MutableInterfaceTypeDefinition type,
        InterfaceTypeDefinitionNode node)
    {
        type.Description = node.Description?.Value;
        BuildComplexType(schema, type, node);
    }

    private static void ExtendInterfaceType(
        MutableSchemaDefinition schema,
        MutableInterfaceTypeDefinition type,
        InterfaceTypeExtensionNode node)
        => BuildComplexType(schema, type, node);

    private static void BuildComplexType(
        MutableSchemaDefinition schema,
        MutableComplexTypeDefinition type,
        ComplexTypeDefinitionNodeBase node)
    {
        BuildDirectiveCollection(schema, type.Directives, node.Directives);

        foreach (var interfaceRef in node.Interfaces)
        {
            type.Implements.Add(schema.Types.BuildType<MutableInterfaceTypeDefinition>(interfaceRef));
        }

        foreach (var fieldNode in node.Fields)
        {
            if (type.Fields.ContainsName(fieldNode.Name.Value))
            {
                // todo : parser error
                throw new Exception("");
            }

            var field = new MutableOutputFieldDefinition(fieldNode.Name.Value);
            field.Description = fieldNode.Description?.Value;
            field.Type = schema.Types.BuildType(fieldNode.Type).ExpectOutputType();

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

                var argument = new MutableInputFieldDefinition(argumentNode.Name.Value);
                argument.Description = argumentNode.Description?.Value;
                argument.Type = schema.Types.BuildType(argumentNode.Type).ExpectInputType();
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
        MutableSchemaDefinition schema,
        MutableInputObjectTypeDefinition type,
        InputObjectTypeDefinitionNode node)
    {
        type.Description = node.Description?.Value;
        ExtendInputObjectType(schema, type, node);
    }

    private static void ExtendInputObjectType(
        MutableSchemaDefinition schema,
        MutableInputObjectTypeDefinition type,
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

            var field = new MutableInputFieldDefinition(fieldNode.Name.Value);
            field.Description = fieldNode.Description?.Value;
            field.Type = schema.Types.BuildType(fieldNode.Type).ExpectInputType();
            field.DefaultValue = fieldNode.DefaultValue;

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
        MutableSchemaDefinition schema,
        MutableEnumTypeDefinition type,
        EnumTypeDefinitionNode node)
    {
        type.Description = node.Description?.Value;
        ExtendEnumType(schema, type, node);
    }

    private static void ExtendEnumType(
        MutableSchemaDefinition schema,
        MutableEnumTypeDefinition type,
        EnumTypeDefinitionNodeBase node)
    {
        BuildDirectiveCollection(schema, type.Directives, node.Directives);

        foreach (var enumValue in node.Values)
        {
            if (type.Values.ContainsName(enumValue.Name.Value))
            {
                continue;
            }

            var value = new MutableEnumValue(enumValue.Name.Value);
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
        MutableSchemaDefinition schema,
        MutableUnionTypeDefinition type,
        UnionTypeDefinitionNode node)
    {
        type.Description = node.Description?.Value;
        ExtendUnionType(schema, type, node);
    }

    private static void ExtendUnionType(
        MutableSchemaDefinition schema,
        MutableUnionTypeDefinition type,
        UnionTypeDefinitionNodeBase node)
    {
        BuildDirectiveCollection(schema, type.Directives, node.Directives);

        foreach (var objectTypeRef in node.Types)
        {
            var objectType = (MutableObjectTypeDefinition)schema.Types[objectTypeRef.Name.Value];

            if (type.Types.Contains(objectType))
            {
                continue;
            }

            type.Types.Add(objectType);
        }
    }

    private static void BuildScalarType(
        MutableSchemaDefinition schema,
        MutableScalarTypeDefinition type,
        ScalarTypeDefinitionNode node)
    {
        type.Description = node.Description?.Value;
        BuildDirectiveCollection(schema, type.Directives, node.Directives);
    }

    private static void ExtendScalarType(
        MutableSchemaDefinition schema,
        MutableScalarTypeDefinition type,
        ScalarTypeExtensionNode node)
    {
        BuildDirectiveCollection(schema, type.Directives, node.Directives);
    }

    private static void BuildDirectiveTypes(MutableSchemaDefinition schema, DocumentNode document)
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
        MutableSchemaDefinition schema,
        MutableDirectiveDefinition type,
        DirectiveDefinitionNode node)
    {
        type.Description = node.Description?.Value;
        type.IsRepeatable = node.IsRepeatable;

        foreach (var argumentNode in node.Arguments)
        {
            var argument = new MutableInputFieldDefinition(argumentNode.Name.Value);
            argument.Description = argumentNode.Description?.Value;
            argument.Type = schema.Types.BuildType(argumentNode.Type).ExpectInputType();
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
        MutableSchemaDefinition schema,
        SchemaDefinitionNode node)
    {
        schema.Description = node.Description?.Value;
        ExtendSchema(schema, node);
    }

    private static void ExtendSchema(
        MutableSchemaDefinition schema,
        SchemaDefinitionNodeBase node)
    {
        BuildDirectiveCollection(schema, schema.Directives, node.Directives);

        foreach (var operationType in node.OperationTypes)
        {
            var typeName = operationType.Type.Name.Value;

            switch (operationType.Operation)
            {
                case OperationType.Query:
                    schema.QueryType = (MutableObjectTypeDefinition)schema.Types[typeName];
                    break;

                case OperationType.Mutation:
                    schema.MutationType = (MutableObjectTypeDefinition)schema.Types[typeName];
                    break;

                case OperationType.Subscription:
                    schema.SubscriptionType = (MutableObjectTypeDefinition)schema.Types[typeName];
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private static void BuildDirectiveCollection(
        MutableSchemaDefinition schema,
        DirectiveCollection directives,
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
                else
                {
                    directiveType = new MutableDirectiveDefinition(directiveNode.Name.Value);
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

    private static bool IsDeprecated(DirectiveCollection directives, out string? reason)
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
    public static T BuildType<T>(this TypeDefinitionCollection typesDefinition, ITypeNode typeRef)
        where T : IType
        => (T)BuildType(typesDefinition, typeRef);

    public static IType BuildType(this TypeDefinitionCollection typesDefinition, ITypeNode typeRef)
    {
        switch (typeRef)
        {
            case NonNullTypeNode nonNullTypeRef:
                return new NonNullType(BuildType(typesDefinition, nonNullTypeRef.Type));

            case ListTypeNode listTypeRef:
                return new ListType(BuildType(typesDefinition, listTypeRef.Type));

            case NamedTypeNode namedTypeRef:
                if (typesDefinition.TryGetType(namedTypeRef.Name.Value, out var type))
                {
                    return type;
                }

                if (BuiltIns.IsBuiltInScalar(namedTypeRef.Name.Value))
                {
                    var scalar = new MutableScalarTypeDefinition(namedTypeRef.Name.Value) { IsSpecScalar = true };
                    typesDefinition.Add(scalar);
                    return scalar;
                }

                var missing = new MissingType(namedTypeRef.Name.Value);
                typesDefinition.Add(missing);
                return missing;

            default:
                // TODO : parsing error
                throw new ArgumentOutOfRangeException(nameof(typeRef));
        }
    }
}
