using HotChocolate.Language;
using HotChocolate.Utilities.Introspection.Properties;

namespace HotChocolate.Utilities.Introspection;

/// <summary>
/// A utility to format an introspection result into a GraphQL schema document.
/// </summary>
internal static class IntrospectionFormatter
{
    public static DocumentNode Format(IntrospectionResult result)
    {
        var typeDefinitions = new List<IDefinitionNode>();
        typeDefinitions.Add(CreateSchema(result.Data!.Schema));
        typeDefinitions.AddRange(CreateTypes(result.Data.Schema.Types));

        foreach (var directive in result.Data.Schema.Directives)
        {
            var directiveDefinition = CreateDirectiveDefinition(directive);
            if (directiveDefinition.Locations.Any())
            {
                typeDefinitions.Add(directiveDefinition);
            }
        }

        return new DocumentNode(typeDefinitions);
    }

    private static SchemaDefinitionNode CreateSchema(Schema schema)
    {
        var operations = new List<OperationTypeDefinitionNode>();

        AddRootTypeRef(
            schema.QueryType,
            OperationType.Query,
            operations);

        AddRootTypeRef(
            schema.MutationType,
            OperationType.Mutation,
            operations);

        AddRootTypeRef(
            schema.SubscriptionType,
            OperationType.Subscription,
            operations);

        return new SchemaDefinitionNode
        (
            null,
            null,
            Array.Empty<DirectiveNode>(),
            operations
        );
    }

    private static void AddRootTypeRef(
        RootTypeRef rootType,
        OperationType operation,
        ICollection<OperationTypeDefinitionNode> operations)
    {
        if (rootType is { Name: not null, })
        {
            operations.Add(new OperationTypeDefinitionNode(
                null,
                operation,
                new NamedTypeNode(new NameNode(rootType.Name))));
        }
    }

    private static IEnumerable<ITypeDefinitionNode> CreateTypes(
        ICollection<FullType> types)
    {
        foreach (var type in types)
        {
            yield return CreateTypes(type);
        }
    }

    private static ITypeDefinitionNode CreateTypes(FullType type)
    {
        switch (type.Kind)
        {
            case TypeKind.ENUM:
                return CreateEnumType(type);

            case TypeKind.INPUT_OBJECT:
                return CreateInputObject(type);

            case TypeKind.INTERFACE:
                return CreateInterface(type);

            case TypeKind.OBJECT:
                return CreateObject(type);

            case TypeKind.SCALAR:
                return CreateScalar(type);

            case TypeKind.UNION:
                return CreateUnion(type);

            default:
                throw new NotSupportedException(
                    IntroResources.Type_NotSupported);
        }
    }

    private static EnumTypeDefinitionNode CreateEnumType(FullType type)
    {
        return new EnumTypeDefinitionNode
        (
            null,
            new NameNode(type.Name),
            CreateDescription(type.Description),
            Array.Empty<DirectiveNode>(),
            CreateEnumValues(type.EnumValues)
        );
    }

    private static IReadOnlyList<EnumValueDefinitionNode> CreateEnumValues(
        IEnumerable<EnumValue> enumValues)
    {
        var values = new List<EnumValueDefinitionNode>();

        foreach (var value in enumValues)
        {
            values.Add(new EnumValueDefinitionNode
            (
                null,
                new NameNode(value.Name),
                CreateDescription(value.Description),
                CreateDeprecatedDirective(
                    value.IsDeprecated,
                    value.DeprecationReason)
            ));
        }

        return values;
    }

    private static InputObjectTypeDefinitionNode CreateInputObject(
        FullType type)
    {
        return new InputObjectTypeDefinitionNode
        (
            null,
            new NameNode(type.Name),
            CreateDescription(type.Description),
            Array.Empty<DirectiveNode>(),
            CreateInputValues(type.InputFields)
        );
    }

    private static IReadOnlyList<InputValueDefinitionNode> CreateInputValues(
        IEnumerable<InputField> fields)
    {
        var list = new List<InputValueDefinitionNode>();

        foreach (var field in fields)
        {
            list.Add(new InputValueDefinitionNode
            (
                null,
                new NameNode(field.Name),
                CreateDescription(field.Description),
                CreateTypeReference(field.Type),
                ParseDefaultValue(field.DefaultValue),
                CreateDeprecatedDirective(
                    field.IsDeprecated,
                    field.DeprecationReason)
            ));
        }

        return list;
    }

    private static InterfaceTypeDefinitionNode CreateInterface(
        FullType type)
    {
        return new InterfaceTypeDefinitionNode
        (
            null,
            new NameNode(type.Name),
            CreateDescription(type.Description),
            Array.Empty<DirectiveNode>(),
            Array.Empty<NamedTypeNode>(),
            CreateFields(type.Fields)
        );
    }

    private static ObjectTypeDefinitionNode CreateObject(
        FullType type)
    {
        return new ObjectTypeDefinitionNode
        (
            null,
            new NameNode(type.Name),
            CreateDescription(type.Description),
            Array.Empty<DirectiveNode>(),
            CreateNamedTypeRefs(type.Interfaces),
            CreateFields(type.Fields)
        );
    }

    private static IReadOnlyList<FieldDefinitionNode> CreateFields(
        IEnumerable<Field> fields)
    {
        var list = new List<FieldDefinitionNode>();

        foreach (var field in fields)
        {
            list.Add(new FieldDefinitionNode
            (
                null,
                new NameNode(field.Name),
                CreateDescription(field.Description),
                CreateInputValues(field.Args),
                CreateTypeReference(field.Type),
                CreateDeprecatedDirective(
                    field.IsDeprecated,
                    field.DeprecationReason)
            ));
        }
        return list;
    }

    private static UnionTypeDefinitionNode CreateUnion(
        FullType type)
    {
        return new UnionTypeDefinitionNode
        (
            null,
            new NameNode(type.Name),
            CreateDescription(type.Description),
            Array.Empty<DirectiveNode>(),
            CreateNamedTypeRefs(type.PossibleTypes)
        );
    }

    private static ScalarTypeDefinitionNode CreateScalar(
        FullType type)
    {
        return new ScalarTypeDefinitionNode
        (
            null,
            new NameNode(type.Name),
            CreateDescription(type.Description),
            Array.Empty<DirectiveNode>()
        );
    }

    private static DirectiveDefinitionNode CreateDirectiveDefinition(
        Directive directive)
    {
        var locations = directive.Locations?.Select(t => new NameNode(t)).ToList() ??
            InferDirectiveLocation(directive);

        return new DirectiveDefinitionNode
        (
            null,
            new NameNode(directive.Name),
            CreateDescription(directive.Description),
            directive.IsRepeatable ?? false,
            CreateInputValues(directive.Args),
            locations
        );
    }

    private static IReadOnlyList<NameNode> InferDirectiveLocation(
        Directive directive)
    {
        var locations = new List<NameNode>();

        if (directive.OnField)
        {
            locations.Add(new NameNode(
                DirectiveLocation.Field.ToString()));
        }

        if (directive.OnFragment)
        {
            locations.Add(new NameNode(
                DirectiveLocation.FieldDefinition.ToString()));
            locations.Add(new NameNode(
                DirectiveLocation.InlineFragment.ToString()));
            locations.Add(new NameNode(
                DirectiveLocation.FragmentSpread.ToString()));
        }

        if (directive.OnOperation)
        {
            locations.Add(new NameNode(
                DirectiveLocation.Query.ToString()));
            locations.Add(new NameNode(
                DirectiveLocation.Mutation.ToString()));
            locations.Add(new NameNode(
                DirectiveLocation.Subscription.ToString()));
        }

        return locations;
    }

    private static IReadOnlyList<NamedTypeNode> CreateNamedTypeRefs(
        IEnumerable<TypeRef> interfaces)
    {
        var list = new List<NamedTypeNode>();

        foreach (var typeRef in interfaces)
        {
            list.Add(new NamedTypeNode(new NameNode(typeRef.Name)));
        }

        return list;
    }

    private static IReadOnlyList<DirectiveNode> CreateDeprecatedDirective(
        bool isDeprecated, string deprecationReason)
    {
        if (isDeprecated)
        {
            return new List<DirectiveNode>
            {
                new DirectiveNode
                (
                    WellKnownDirectives.Deprecated,
                    new ArgumentNode
                    (
                        WellKnownDirectives.DeprecationReasonArgument,
                        new StringValueNode(deprecationReason)
                    )
                ),
            };
        }
        return Array.Empty<DirectiveNode>();
    }

    private static StringValueNode? CreateDescription(string description)
    {
        return string.IsNullOrEmpty(description)
            ? null
            : new StringValueNode(description);
    }

    private static IValueNode ParseDefaultValue(string defaultValue)
    {
        return !string.IsNullOrEmpty(defaultValue)
            ? Utf8GraphQLParser.Syntax.ParseValueLiteral(defaultValue)
            : NullValueNode.Default;
    }

    private static ITypeNode CreateTypeReference(TypeRef typeRef)
    {
        if (typeRef.Kind == TypeKind.NON_NULL)
        {
            return new NonNullTypeNode
            (
                (INullableTypeNode)CreateTypeReference(typeRef.OfType)
            );
        }

        if (typeRef.Kind == TypeKind.LIST)
        {
            return new ListTypeNode
            (
                CreateTypeReference(typeRef.OfType)
            );
        }

        return new NamedTypeNode(new NameNode(typeRef.Name));
    }
}
