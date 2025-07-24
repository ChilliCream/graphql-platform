using System.Collections.Immutable;
using HotChocolate.Features;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Directives;
using HotChocolate.Fusion.Utilities;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using static System.Runtime.InteropServices.ImmutableCollectionsMarshal;

namespace HotChocolate.Fusion.Types.Completion;

internal static class CompositeSchemaBuilder
{
    public static FusionSchemaDefinition Create(
        string name,
        DocumentNode documentNode,
        IServiceProvider? services = null,
        IFeatureCollection? features = null)
    {
        services ??= EmptyServiceProvider.Instance;
        var typeInterceptor = CreateTypeInterceptor(services);
        var context = CreateTypes(name, documentNode, services, features, typeInterceptor);
        return CompleteTypes(context);
    }

    private static CompositeSchemaBuilderContext CreateTypes(
        string name,
        DocumentNode schema,
        IServiceProvider services,
        IFeatureCollection? features,
        CompositeTypeInterceptor typeInterceptor)
    {
        string? description = null;
        var queryType = "Query";
        string? mutationType = null;
        string? subscriptionType = null;
        var directives = ImmutableArray<DirectiveNode>.Empty;
        var types = ImmutableArray.CreateBuilder<ITypeDefinition>();
        var typeDefinitions = ImmutableDictionary.CreateBuilder<string, ITypeDefinitionNode>();
        var directiveTypes = ImmutableArray.CreateBuilder<FusionDirectiveDefinition>();
        var directiveDefinitions = ImmutableDictionary.CreateBuilder<string, DirectiveDefinitionNode>();

        var schemaDefinition = schema.Definitions.OfType<SchemaDefinitionNode>().FirstOrDefault();
        if (schemaDefinition is not null)
        {
            description = schemaDefinition.Description?.Value;
            directives = [.. schemaDefinition.Directives];

            foreach (var operationType in schemaDefinition.OperationTypes)
            {
                switch (operationType.Operation)
                {
                    case OperationType.Query:
                        queryType = operationType.Type.Name.Value;
                        break;

                    case OperationType.Mutation:
                        mutationType = operationType.Type.Name.Value;
                        break;

                    case OperationType.Subscription:
                        subscriptionType = operationType.Type.Name.Value;
                        break;
                }
            }
        }

        foreach (var definition in IntrospectionSchema.Document.Definitions.Concat(schema.Definitions))
        {
            if (definition is IHasName namedSyntaxNode
                && (FusionBuiltIns.IsBuiltInType(namedSyntaxNode.Name.Value)
                    || FusionBuiltIns.IsBuiltInDirective(namedSyntaxNode.Name.Value)))
            {
                continue;
            }

            switch (definition)
            {
                case ObjectTypeDefinitionNode objectType:
                    var type = CreateObjectType(
                        objectType,
                        objectType.Name.Value.Equals(queryType, StringComparison.Ordinal));
                    types.Add(type);
                    typeDefinitions.Add(objectType.Name.Value, objectType);
                    break;

                case InterfaceTypeDefinitionNode interfaceType:
                    types.Add(CreateInterfaceType(interfaceType));
                    typeDefinitions.Add(interfaceType.Name.Value, interfaceType);
                    break;

                case UnionTypeDefinitionNode unionType:
                    types.Add(CreateUnionType(unionType));
                    typeDefinitions.Add(unionType.Name.Value, unionType);
                    break;

                case InputObjectTypeDefinitionNode inputObjectType:
                    types.Add(CreateInputObjectType(inputObjectType));
                    typeDefinitions.Add(inputObjectType.Name.Value, inputObjectType);
                    break;

                case EnumTypeDefinitionNode enumType:
                    types.Add(CreateEnumType(enumType));
                    typeDefinitions.Add(enumType.Name.Value, enumType);
                    break;

                case ScalarTypeDefinitionNode scalarType:
                    types.Add(CreateScalarType(scalarType));
                    typeDefinitions.Add(scalarType.Name.Value, scalarType);
                    break;

                case DirectiveDefinitionNode directiveType:
                    directiveTypes.Add(CreateDirectiveType(directiveType));
                    directiveDefinitions.Add(directiveType.Name.Value, directiveType);
                    break;
            }
        }

        features ??= FeatureCollection.Empty;

        return new CompositeSchemaBuilderContext(
            name,
            description,
            services,
            queryType,
            mutationType,
            subscriptionType,
            directives,
            types.ToImmutable(),
            typeDefinitions.ToImmutable(),
            directiveTypes.ToImmutable(),
            directiveDefinitions.ToImmutable(),
            features.ToReadOnly(),
            CreateTypeInterceptor(services));
    }

    private static CompositeTypeInterceptor CreateTypeInterceptor(IServiceProvider services)
    {
        var interceptors = services.GetService<IEnumerable<CompositeTypeInterceptor>>()?.ToArray() ?? [];

        return interceptors.Length switch
        {
            0 => new NoOpCompositeTypeInterceptor(),
            1 => interceptors[0],
            _ => new AggregateCompositeTypeInterceptor(interceptors)
        };
    }

    private static FusionObjectTypeDefinition CreateObjectType(
        ObjectTypeDefinitionNode definition,
        bool isQuery)
    {
        return new FusionObjectTypeDefinition(
            definition.Name.Value,
            definition.Description?.Value,
            CreateOutputFields(definition.Fields, isQuery));
    }

    private static FusionInterfaceTypeDefinition CreateInterfaceType(
        InterfaceTypeDefinitionNode definition)
    {
        return new FusionInterfaceTypeDefinition(
            definition.Name.Value,
            definition.Description?.Value,
            CreateOutputFields(definition.Fields, false));
    }

    private static FusionUnionTypeDefinition CreateUnionType(
        UnionTypeDefinitionNode definition)
    {
        return new FusionUnionTypeDefinition(
            definition.Name.Value,
            definition.Description?.Value);
    }

    private static FusionInputObjectTypeDefinition CreateInputObjectType(
        InputObjectTypeDefinitionNode definition)
    {
        return new FusionInputObjectTypeDefinition(
            definition.Name.Value,
            definition.Description?.Value,
            CreateInputFields(definition.Fields));
    }

    private static FusionEnumTypeDefinition CreateEnumType(
        EnumTypeDefinitionNode definition)
    {
        return new FusionEnumTypeDefinition(
            definition.Name.Value,
            definition.Description?.Value,
            CreateEnumValues(definition.Values));
    }

    private static FusionScalarTypeDefinition CreateScalarType(
        ScalarTypeDefinitionNode definition)
    {
        return new FusionScalarTypeDefinition(
            definition.Name.Value,
            definition.Description?.Value);
    }

    private static FusionDirectiveDefinition CreateDirectiveType(
        DirectiveDefinitionNode definition)
    {
        return new FusionDirectiveDefinition(
            definition.Name.Value,
            definition.Description?.Value,
            definition.IsRepeatable,
            CreateInputFields(definition.Arguments),
            DirectiveLocationUtils.Parse(definition.Locations));
    }

    private static FusionOutputFieldDefinitionCollection CreateOutputFields(
        IReadOnlyList<FieldDefinitionNode> fields,
        bool isQuery)
    {
        var size = isQuery ? fields.Count + 3 : fields.Count;
        var sourceFields = new FusionOutputFieldDefinition[size];

        if (isQuery)
        {
            sourceFields[0] = new FusionOutputFieldDefinition(
                "__schema",
                null,
                isDeprecated: false,
                deprecationReason: null,
                arguments: FusionInputFieldDefinitionCollection.Empty);

            sourceFields[1] = new FusionOutputFieldDefinition(
                "__type",
                null,
                isDeprecated: false,
                deprecationReason: null,
                arguments: new FusionInputFieldDefinitionCollection(
                [
                    new FusionInputFieldDefinition(
                        "name",
                        null,
                        null,
                        isDeprecated: false,
                        deprecationReason:  null)
                ]));

            sourceFields[2] = new FusionOutputFieldDefinition(
                "__typename",
                null,
                isDeprecated: false,
                deprecationReason: null,
                arguments: FusionInputFieldDefinitionCollection.Empty);

            for (var i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                var isDeprecated = DeprecatedDirectiveParser.TryParse(field.Directives, out var deprecated);

                sourceFields[i + 3] = new FusionOutputFieldDefinition(
                    field.Name.Value,
                    field.Description?.Value,
                    isDeprecated,
                    deprecated?.Reason,
                    CreateOutputFieldArguments(field.Arguments));
            }
        }
        else
        {
            for (var i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                var isDeprecated = DeprecatedDirectiveParser.TryParse(field.Directives, out var deprecated);

                sourceFields[i] = new FusionOutputFieldDefinition(
                    field.Name.Value,
                    field.Description?.Value,
                    isDeprecated,
                    deprecated?.Reason,
                    CreateOutputFieldArguments(field.Arguments));
            }
        }

        return new FusionOutputFieldDefinitionCollection(sourceFields);
    }

    private static FusionInputFieldDefinitionCollection CreateOutputFieldArguments(
        IReadOnlyList<InputValueDefinitionNode> arguments)
    {
        if (arguments.Count == 0)
        {
            return FusionInputFieldDefinitionCollection.Empty;
        }

        var temp = new FusionInputFieldDefinition[arguments.Count];

        for (var i = 0; i < arguments.Count; i++)
        {
            var argument = arguments[i];
            var isDeprecated = DeprecatedDirectiveParser.TryParse(argument.Directives, out var deprecated);

            temp[i] = new FusionInputFieldDefinition(
                argument.Name.Value,
                argument.Description?.Value,
                argument.DefaultValue,
                isDeprecated,
                deprecated?.Reason);
        }

        return new FusionInputFieldDefinitionCollection(temp);
    }

    private static FusionInputFieldDefinitionCollection CreateInputFields(
        IReadOnlyList<InputValueDefinitionNode> fields)
    {
        if (fields.Count == 0)
        {
            return FusionInputFieldDefinitionCollection.Empty;
        }

        var sourceFields = new FusionInputFieldDefinition[fields.Count];

        for (var i = 0; i < fields.Count; i++)
        {
            var field = fields[i];
            var isDeprecated = DeprecatedDirectiveParser.TryParse(field.Directives, out var deprecated);

            sourceFields[i] = new FusionInputFieldDefinition(
                field.Name.Value,
                field.Description?.Value,
                field.DefaultValue,
                isDeprecated,
                deprecated?.Reason);
        }

        return new FusionInputFieldDefinitionCollection(sourceFields);
    }

    private static FusionEnumValueCollection CreateEnumValues(
        IReadOnlyList<EnumValueDefinitionNode> fields)
    {
        if (fields.Count == 0)
        {
            return FusionEnumValueCollection.Empty;
        }

        var sourceFields = new FusionEnumValue[fields.Count];

        for (var i = 0; i < fields.Count; i++)
        {
            var field = fields[i];
            var isDeprecated = DeprecatedDirectiveParser.TryParse(field.Directives, out var deprecated);

            sourceFields[i] = new FusionEnumValue(
                field.Name.Value,
                field.Description?.Value,
                isDeprecated,
                deprecated?.Reason);
        }

        return new FusionEnumValueCollection(sourceFields);
    }

    private static FusionSchemaDefinition CompleteTypes(CompositeSchemaBuilderContext context)
    {
        foreach (var type in context.TypeDefinitions)
        {
            switch (type)
            {
                case FusionObjectTypeDefinition objectType:
                    CompleteObjectType(
                        objectType,
                        context.GetTypeDefinition<ObjectTypeDefinitionNode>(objectType.Name),
                        context);
                    break;

                case FusionInterfaceTypeDefinition interfaceType:
                    CompleteInterfaceType(
                        interfaceType,
                        context.GetTypeDefinition<InterfaceTypeDefinitionNode>(interfaceType.Name),
                        context);
                    break;

                case FusionUnionTypeDefinition unionType:
                    CompleteUnionType(
                        unionType,
                        context.GetTypeDefinition<UnionTypeDefinitionNode>(unionType.Name),
                        context);
                    break;

                case FusionInputObjectTypeDefinition inputObjectType:
                    CompleteInputObjectType(
                        inputObjectType,
                        context.GetTypeDefinition<InputObjectTypeDefinitionNode>(inputObjectType.Name),
                        context);
                    break;

                case FusionEnumTypeDefinition enumType:
                    CompleteEnumType(
                        enumType,
                        context.GetTypeDefinition<EnumTypeDefinitionNode>(enumType.Name),
                        context);
                    break;

                case FusionScalarTypeDefinition scalarType:
                    CompleteScalarType(
                        scalarType,
                        context.GetTypeDefinition<ScalarTypeDefinitionNode>(scalarType.Name),
                        context);
                    break;
            }
        }

        foreach (var directiveType in context.DirectiveDefinitions)
        {
            CompleteDirectiveType(
                directiveType,
                context.GetDirectiveDefinition(directiveType.Name),
                context);
        }

        var directives = CompletionTools.CreateDirectiveCollection(context.Directives, context);
        var features = context.Features;

        context.Interceptor.OnCompleteSchema(context, ref features);

        return new FusionSchemaDefinition(
            context.Name,
            context.Description,
            context.Services,
            context.GetType<FusionObjectTypeDefinition>(context.QueryType),
            context.MutationType is not null
                ? context.GetType<FusionObjectTypeDefinition>(context.MutationType)
                : null,
            context.SubscriptionType is not null
                ? context.GetType<FusionObjectTypeDefinition>(context.SubscriptionType)
                : null,
            directives,
            new FusionTypeDefinitionCollection(AsArray(context.TypeDefinitions)!),
            new FusionDirectiveDefinitionCollection(AsArray(context.DirectiveDefinitions)!),
            features.ToReadOnly());
    }

    private static void CompleteObjectType(
        FusionObjectTypeDefinition type,
        ObjectTypeDefinitionNode typeDef,
        CompositeSchemaBuilderContext context)
    {
        var operationType = GetOperationType(typeDef.Name.Value, context);

        foreach (var fieldDef in typeDef.Fields)
        {
            CompleteOutputField(
                type,
                operationType,
                type.Fields[fieldDef.Name.Value],
                fieldDef,
                context);
        }

        if (operationType is OperationType.Query)
        {
            CompleteOutputField(
                type,
                operationType,
                type.Fields["__schema"],
                Utf8GraphQLParser.Syntax.ParseFieldDefinition("__schema: __Schema!"),
                context);

            CompleteOutputField(
                type,
                operationType,
                type.Fields["__type"],
                Utf8GraphQLParser.Syntax.ParseFieldDefinition("__type(name: String!): __Type"),
                context);

            CompleteOutputField(
                type,
                operationType,
                type.Fields["__typename"],
                Utf8GraphQLParser.Syntax.ParseFieldDefinition("__typename: String!"),
                context);
        }

        var directives = CompletionTools.CreateDirectiveCollection(typeDef.Directives, context);
        var interfaces = CompletionTools.CreateInterfaceTypeCollection(typeDef.Interfaces, context);
        var sources = CompletionTools.CreateSourceObjectTypeCollection(typeDef, context);

        type.Complete(
            new CompositeObjectTypeCompletionContext(
                directives,
                interfaces,
                sources,
                FeatureCollection.Empty));
    }

    private static void CompleteInterfaceType(
        FusionInterfaceTypeDefinition type,
        InterfaceTypeDefinitionNode typeDef,
        CompositeSchemaBuilderContext context)
    {
        var operationType = GetOperationType(typeDef.Name.Value, context);

        foreach (var fieldDef in typeDef.Fields)
        {
            CompleteOutputField(
                type,
                operationType,
                type.Fields[fieldDef.Name.Value],
                fieldDef,
                context);
        }

        var directives = CompletionTools.CreateDirectiveCollection(typeDef.Directives, context);
        var interfaces = CompletionTools.CreateInterfaceTypeCollection(typeDef.Interfaces, context);
        var sources = CompletionTools.CreateSourceInterfaceTypeCollection(typeDef, context);

        type.Complete(
            new CompositeInterfaceTypeCompletionContext(
                directives,
                interfaces,
                sources,
                FeatureCollection.Empty));
    }

    private static void CompleteUnionType(
        FusionUnionTypeDefinition type,
        UnionTypeDefinitionNode typeDef,
        CompositeSchemaBuilderContext context)
    {
        var directives = CompletionTools.CreateDirectiveCollection(typeDef.Directives, context);
        var types = CompletionTools.CreateObjectTypeCollection(typeDef.Types, context);
        type.Complete(new CompositeUnionTypeCompletionContext(types, directives, FeatureCollection.Empty));
    }

    private static void CompleteOutputField(
        FusionComplexTypeDefinition declaringType,
        OperationType? operationType,
        FusionOutputFieldDefinition fieldDefinition,
        FieldDefinitionNode fieldDef,
        CompositeSchemaBuilderContext context)
    {
        foreach (var argumentDef in fieldDef.Arguments)
        {
            CompleteInputField(
                fieldDefinition,
                fieldDefinition.Arguments[argumentDef.Name.Value],
                argumentDef,
                context);
        }

        var directives = CompletionTools.CreateDirectiveCollection(fieldDef.Directives, context);
        var type = context.GetType(fieldDef.Type).ExpectOutputType();
        var sources = BuildSourceObjectFieldCollection(fieldDefinition, fieldDef, context);
        var features = FeatureCollection.Empty;

        context.Interceptor.OnCompleteOutputField(
            context,
            declaringType,
            fieldDefinition,
            operationType,
            ref features);

        fieldDefinition.Complete(
            new CompositeObjectFieldCompletionContext(
                declaringType,
                directives,
                type,
                sources,
                features));
    }

    private static SourceObjectFieldCollection BuildSourceObjectFieldCollection(
        FusionOutputFieldDefinition fieldDefinition,
        FieldDefinitionNode fieldDef,
        CompositeSchemaBuilderContext context)
    {
        var fieldDirectives = FieldDirectiveParser.Parse(fieldDef.Directives);
        var requireDirectives = RequiredDirectiveParser.Parse(fieldDef.Directives);
        var temp = ImmutableArray.CreateBuilder<SourceOutputField>();

        foreach (var fieldDirective in fieldDirectives)
        {
            temp.Add(
                new SourceOutputField(
                    fieldDirective.SourceName ?? fieldDefinition.Name,
                    fieldDirective.SchemaName,
                    ParseRequirements(requireDirectives, fieldDirective.SchemaName),
                    CompleteType(fieldDef.Type, fieldDirective.SourceType, context)));
        }

        return new SourceObjectFieldCollection(temp.ToImmutable());

        static FieldRequirements? ParseRequirements(
            ImmutableArray<RequireDirective> requireDirectives,
            string schemaName)
        {
            var requireDirective = requireDirectives.FirstOrDefault(t => t.SchemaName == schemaName);

            if (requireDirective is not null)
            {
                var argumentsBuilder = ImmutableArray.CreateBuilder<RequiredArgument>();

                foreach (var argument in requireDirective.Field.Arguments)
                {
                    argumentsBuilder.Add(new RequiredArgument(argument.Name.Value, argument.Type));
                }

                var fieldsBuilder = ImmutableArray.CreateBuilder<FieldPath?>();

                foreach (var field in requireDirective.Map)
                {
                    fieldsBuilder.Add(field is not null ? FieldPath.Parse(field) : null);
                }

                var arguments = argumentsBuilder.ToImmutable();
                var fields = fieldsBuilder.ToImmutable();
                var selectionSet = fields.ToSelectionSetNode();

                return new FieldRequirements(schemaName, arguments, fields, selectionSet);
            }

            return null;
        }

        static IType CompleteType(
            ITypeNode type,
            ITypeNode? sourceType,
            CompositeSchemaBuilderContext context)
        {
            return sourceType is null
                ? context.GetType(type)
                : context.GetType(sourceType, type.NamedType().Name.Value);
        }
    }

    private static void CompleteInputObjectType(
        FusionInputObjectTypeDefinition inputObjectType,
        InputObjectTypeDefinitionNode inputObjectTypeDef,
        CompositeSchemaBuilderContext context)
    {
        foreach (var fieldDef in inputObjectTypeDef.Fields)
        {
            CompleteInputField(inputObjectType, inputObjectType.Fields[fieldDef.Name.Value], fieldDef, context);
        }

        var directives = CompletionTools.CreateDirectiveCollection(inputObjectTypeDef.Directives, context);
        inputObjectType.Complete(new CompositeInputObjectTypeCompletionContext(directives, FeatureCollection.Empty));
    }

    private static void CompleteInputField(
        ITypeSystemMember declaringMember,
        FusionInputFieldDefinition inputField,
        InputValueDefinitionNode argumentDef,
        CompositeSchemaBuilderContext context)
    {
        var directives = CompletionTools.CreateDirectiveCollection(argumentDef.Directives, context);
        var type = context.GetType(argumentDef.Type).ExpectInputType();

        inputField.Complete(
            new CompositeInputFieldCompletionContext(
                declaringMember,
                directives,
                type,
                FeatureCollection.Empty));
    }

    private static void CompleteEnumType(
        FusionEnumTypeDefinition typeDefinition,
        EnumTypeDefinitionNode typeDefinitionNode,
        CompositeSchemaBuilderContext context)
    {
        var directives = CompletionTools.CreateDirectiveCollection(typeDefinitionNode.Directives, context);

        foreach (var value in typeDefinitionNode.Values)
        {
            CompleteEnumValue(
                typeDefinition,
                typeDefinition.Values[value.Name.Value],
                value,
                context);
        }

        typeDefinition.Complete(
            new CompositeEnumTypeCompletionContext(
                directives,
                FeatureCollection.Empty));
    }

    private static void CompleteEnumValue(
        IEnumTypeDefinition declaringType,
        FusionEnumValue enumValue,
        EnumValueDefinitionNode enumValueDef,
        CompositeSchemaBuilderContext context)
    {
        var directives = CompletionTools.CreateDirectiveCollection(enumValueDef.Directives, context);

        enumValue.Complete(
            new CompositeEnumValueCompletionContext(
                declaringType,
                directives,
                FeatureCollection.Empty));
    }

    private static void CompleteScalarType(
        FusionScalarTypeDefinition typeDefinition,
        ScalarTypeDefinitionNode typeDefinitionNode,
        CompositeSchemaBuilderContext context)
    {
        var directives = CompletionTools.CreateDirectiveCollection(typeDefinitionNode.Directives, context);
        var specifiedByDirective = directives.FirstOrDefault("specifiedBy");
        Uri? specifiedBy = null;

        if (specifiedByDirective is not null)
        {
            if (specifiedByDirective.Arguments["url"].Value is not StringValueNode url)
            {
                throw new InvalidOperationException("The specified type does not have a url.");
            }

            specifiedBy = new Uri(url.Value);
        }

        typeDefinition.Complete(
            new CompositeScalarTypeCompletionContext(
                default,
                directives,
                specifiedBy));
    }

    private static void CompleteDirectiveType(
        FusionDirectiveDefinition directiveDefinition,
        DirectiveDefinitionNode directiveDefinitionNode,
        CompositeSchemaBuilderContext context)
    {
        foreach (var argumentDef in directiveDefinitionNode.Arguments)
        {
            CompleteInputField(
                directiveDefinition,
                directiveDefinition.Arguments[argumentDef.Name.Value],
                argumentDef,
                context);
        }
    }

    private static OperationType? GetOperationType(
        string typeName,
        CompositeSchemaBuilderContext context)
    {
        if (context.QueryType.Equals(typeName, StringComparison.OrdinalIgnoreCase))
        {
            return OperationType.Query;
        }

        if (context.MutationType?.Equals(typeName, StringComparison.OrdinalIgnoreCase) == true)
        {
            return OperationType.Mutation;
        }

        if (context.SubscriptionType?.Equals(typeName, StringComparison.OrdinalIgnoreCase) == true)
        {
            return OperationType.Subscription;
        }

        return null;
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;

        public static EmptyServiceProvider Instance { get; } = new();
    }

    private enum ComplexType
    {
        Query,
        Object,
        Interface
    }
}
