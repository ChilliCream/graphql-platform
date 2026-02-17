using System.Collections.Immutable;
using HotChocolate.Features;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Rewriters;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Directives;
using HotChocolate.Fusion.Types.Metadata;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using static System.Runtime.InteropServices.ImmutableCollectionsMarshal;
using static HotChocolate.Types.DirectiveNames;

namespace HotChocolate.Fusion.Types.Completion;

internal static class CompositeSchemaBuilder
{
    public static FusionSchemaDefinition Create(
        string name,
        DocumentNode schemaDocument,
        IServiceProvider? services = null,
        IFeatureCollection? features = null)
    {
        services ??= EmptyServiceProvider.Instance;
        var typeInterceptor = CreateTypeInterceptor(services);
        var options = FusionSchemaOptions.From(features?.Get<IFusionSchemaOptions>());
        var context = CreateTypes(name, schemaDocument, services, features, options, typeInterceptor);
        return CompleteTypes(context, options);
    }

    private static CompositeSchemaBuilderContext CreateTypes(
        string name,
        DocumentNode schemaDocument,
        IServiceProvider services,
        IFeatureCollection? features,
        FusionSchemaOptions options,
        CompositeTypeInterceptor typeInterceptor)
    {
        string? description = null;
        var queryType = "Query";
        string? mutationType = null;
        string? subscriptionType = null;
        var directives = ImmutableArray<DirectiveNode>.Empty;
        var types = ImmutableArray.CreateBuilder<IFusionTypeDefinition>();
        var typeDefinitions = ImmutableDictionary.CreateBuilder<string, ITypeDefinitionNode>();
        var directiveTypes = ImmutableArray.CreateBuilder<FusionDirectiveDefinition>();
        var directiveDefinitions = ImmutableDictionary.CreateBuilder<string, DirectiveDefinitionNode>();

        var schemaDefinition = schemaDocument.Definitions.OfType<SchemaDefinitionNode>().FirstOrDefault();
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

        foreach (var definition in IntrospectionSchema.Document.Definitions.Concat(schemaDocument.Definitions))
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
                    if (options.ApplySerializeAsToScalars || !directiveType.Name.Value.Equals(SerializeAs.Name))
                    {
                        directiveTypes.Add(CreateDirectiveType(directiveType));
                        directiveDefinitions.Add(directiveType.Name.Value, directiveType);
                    }
                    break;
            }
        }

        features ??= new FeatureCollection();

        return new CompositeSchemaBuilderContext(
            schemaDocument,
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
            CreateSourceSchemaLookup(schemaDocument),
            features,
            typeInterceptor);
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
        var isInaccessible = InaccessibleDirectiveParser.Parse(definition.Directives);

        return new FusionObjectTypeDefinition(
            definition.Name.Value,
            definition.Description?.Value,
            isInaccessible,
            CreateOutputFields(definition.Fields, isQuery));
    }

    private static FusionInterfaceTypeDefinition CreateInterfaceType(
        InterfaceTypeDefinitionNode definition)
    {
        var isInaccessible = InaccessibleDirectiveParser.Parse(definition.Directives);

        return new FusionInterfaceTypeDefinition(
            definition.Name.Value,
            definition.Description?.Value,
            isInaccessible,
            CreateOutputFields(definition.Fields, false));
    }

    private static FusionUnionTypeDefinition CreateUnionType(
        UnionTypeDefinitionNode definition)
    {
        var isInaccessible = InaccessibleDirectiveParser.Parse(definition.Directives);

        return new FusionUnionTypeDefinition(
            definition.Name.Value,
            definition.Description?.Value,
            isInaccessible);
    }

    private static FusionInputObjectTypeDefinition CreateInputObjectType(
        InputObjectTypeDefinitionNode definition)
    {
        var isInaccessible = InaccessibleDirectiveParser.Parse(definition.Directives);

        return new FusionInputObjectTypeDefinition(
            definition.Name.Value,
            definition.Description?.Value,
            isInaccessible,
            CreateInputFields(definition.Fields));
    }

    private static FusionEnumTypeDefinition CreateEnumType(
        EnumTypeDefinitionNode definition)
    {
        var isInaccessible = InaccessibleDirectiveParser.Parse(definition.Directives);

        return new FusionEnumTypeDefinition(
            definition.Name.Value,
            definition.Description?.Value,
            isInaccessible,
            CreateEnumValues(definition.Values));
    }

    private static FusionScalarTypeDefinition CreateScalarType(
        ScalarTypeDefinitionNode definition)
    {
        var isInaccessible = InaccessibleDirectiveParser.Parse(definition.Directives);

        return new FusionScalarTypeDefinition(
            definition.Name.Value,
            definition.Description?.Value,
            isInaccessible);
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
                IntrospectionFieldNames.Schema,
                null,
                isDeprecated: false,
                deprecationReason: null,
                isInaccessible: false,
                arguments: FusionInputFieldDefinitionCollection.Empty);

            sourceFields[1] = new FusionOutputFieldDefinition(
                IntrospectionFieldNames.Type,
                null,
                isDeprecated: false,
                deprecationReason: null,
                isInaccessible: false,
                arguments: new FusionInputFieldDefinitionCollection(
                [
                    new FusionInputFieldDefinition(
                        0,
                        "name",
                        null,
                        null,
                        isDeprecated: false,
                        deprecationReason: null,
                        isInaccessible: false)
                ]));

            sourceFields[2] = new FusionOutputFieldDefinition(
                IntrospectionFieldNames.TypeName,
                null,
                isDeprecated: false,
                deprecationReason: null,
                isInaccessible: false,
                arguments: FusionInputFieldDefinitionCollection.Empty);

            for (var i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                var isDeprecated = DeprecatedDirectiveParser.TryParse(field.Directives, out var deprecated);
                var isInaccessible = InaccessibleDirectiveParser.Parse(field.Directives);

                sourceFields[i + 3] = new FusionOutputFieldDefinition(
                    field.Name.Value,
                    field.Description?.Value,
                    isDeprecated,
                    deprecated?.Reason,
                    isInaccessible: isInaccessible,
                    CreateOutputFieldArguments(field.Arguments));
            }
        }
        else
        {
            for (var i = 0; i < fields.Count; i++)
            {
                var field = fields[i];
                var isDeprecated = DeprecatedDirectiveParser.TryParse(field.Directives, out var deprecated);
                var isInaccessible = InaccessibleDirectiveParser.Parse(field.Directives);

                sourceFields[i] = new FusionOutputFieldDefinition(
                    field.Name.Value,
                    field.Description?.Value,
                    isDeprecated,
                    deprecated?.Reason,
                    isInaccessible: isInaccessible,
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
            var isInaccessible = InaccessibleDirectiveParser.Parse(argument.Directives);

            temp[i] = new FusionInputFieldDefinition(
                i,
                argument.Name.Value,
                argument.Description?.Value,
                argument.DefaultValue,
                isDeprecated,
                deprecated?.Reason,
                isInaccessible);
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
            var isInaccessible = InaccessibleDirectiveParser.Parse(field.Directives);

            sourceFields[i] = new FusionInputFieldDefinition(
                i,
                field.Name.Value,
                field.Description?.Value,
                field.DefaultValue,
                isDeprecated,
                deprecated?.Reason,
                isInaccessible);
        }

        return new FusionInputFieldDefinitionCollection(sourceFields);
    }

    private static FusionEnumValueCollection CreateEnumValues(
        IReadOnlyList<EnumValueDefinitionNode> values)
    {
        if (values.Count == 0)
        {
            return FusionEnumValueCollection.Empty;
        }

        var sourceFields = new FusionEnumValue[values.Count];

        for (var i = 0; i < values.Count; i++)
        {
            var value = values[i];
            var isDeprecated = DeprecatedDirectiveParser.TryParse(value.Directives, out var deprecated);
            var isInaccessible = InaccessibleDirectiveParser.Parse(value.Directives);

            sourceFields[i] = new FusionEnumValue(
                value.Name.Value,
                value.Description?.Value,
                isDeprecated,
                deprecated?.Reason,
                isInaccessible);
        }

        return new FusionEnumValueCollection(sourceFields);
    }

    private static FusionSchemaDefinition CompleteTypes(
        CompositeSchemaBuilderContext context,
        FusionSchemaOptions options)
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
                        options,
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

        context.Interceptor.OnBeforeCompleteSchema(context, ref features);
        features.Set<ValueSelectionToSelectionSetRewriter>(null);

        var nodeFallbackLookup = new NodeFallbackLookup();
        context.RegisterForCompletion(nodeFallbackLookup);
        features.Set(nodeFallbackLookup);

        var schema = new FusionSchemaDefinition(
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
            features);

        context.Interceptor.OnAfterCompleteSchema(context, schema);
        schema.Seal();
        context.Complete(schema);
        schema.InitializePlannerTopologyCache();

        return schema;
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
                type.Fields.GetField(fieldDef.Name.Value, allowInaccessibleFields: true),
                fieldDef,
                context);
        }

        if (operationType is OperationType.Query)
        {
            CompleteOutputField(
                type,
                operationType,
                type.Fields[IntrospectionFieldNames.Schema],
                Utf8GraphQLParser.Syntax.ParseFieldDefinition("__schema: __Schema!"),
                context);

            CompleteOutputField(
                type,
                operationType,
                type.Fields[IntrospectionFieldNames.Type],
                Utf8GraphQLParser.Syntax.ParseFieldDefinition("__type(name: String!): __Type"),
                context);

            CompleteOutputField(
                type,
                operationType,
                type.Fields[IntrospectionFieldNames.TypeName],
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
                type.Fields.GetField(fieldDef.Name.Value, allowInaccessibleFields: true),
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
        var sources = CompletionTools.CreateSourceUnionTypeCollection(typeDef, context);
        type.Complete(new CompositeUnionTypeCompletionContext(types, directives, sources, FeatureCollection.Empty));
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
                fieldDefinition.Arguments.GetField(argumentDef.Name.Value, allowInaccessibleFields: true),
                argumentDef,
                context);
        }

        var directives = CompletionTools.CreateDirectiveCollection(fieldDef.Directives, context);
        var type = context.GetType(fieldDef.Type).ExpectOutputType();
        var sources = BuildSourceOutputFieldCollection(declaringType.Name, fieldDefinition, fieldDef, context);
        var features = FeatureCollection.Empty;

        context.Interceptor.OnCompleteOutputField(
            context,
            declaringType,
            fieldDefinition,
            operationType,
            ref features);

        fieldDefinition.Complete(
            new CompositeOutputFieldCompletionContext(
                declaringType,
                directives,
                type,
                sources,
                features));
    }

    private static SourceObjectFieldCollection BuildSourceOutputFieldCollection(
        string declaringTypeName,
        FusionOutputFieldDefinition fieldDefinition,
        FieldDefinitionNode fieldDef,
        CompositeSchemaBuilderContext context)
    {
        var fieldDirectives = FieldDirectiveParser.Parse(fieldDef.Directives);
        var requireDirectives = RequiredDirectiveParser.Parse(fieldDef.Directives);
        var temp = ImmutableArray.CreateBuilder<SourceOutputField>();

        foreach (var fieldDirective in fieldDirectives)
        {
            var requirements = ParseRequirements(
                declaringTypeName,
                requireDirectives,
                fieldDirective.SchemaKey,
                context);

            if (requirements is not null)
            {
                context.RegisterForCompletion(requirements);
            }

            temp.Add(
                new SourceOutputField(
                    fieldDirective.SourceName ?? fieldDefinition.Name,
                    context.GetSchemaName(fieldDirective.SchemaKey),
                    requirements,
                    CompleteType(fieldDef.Type, fieldDirective.SourceType, context)));
        }

        return new SourceObjectFieldCollection(temp.ToImmutable());

        static FieldRequirements? ParseRequirements(
            string declaringTypeName,
            ImmutableArray<RequireDirective> requireDirectives,
            SchemaKey schemaKey,
            CompositeSchemaBuilderContext context)
        {
            var requireDirective = requireDirectives.FirstOrDefault(t => t.SchemaKey.Equals(schemaKey));

            if (requireDirective is not null)
            {
                var argumentsBuilder = ImmutableArray.CreateBuilder<RequiredArgument>();

                foreach (var argument in requireDirective.Field.Arguments)
                {
                    argumentsBuilder.Add(new RequiredArgument(argument.Name.Value, argument.Type));
                }

                var fieldsBuilder = ImmutableArray.CreateBuilder<IValueSelectionNode?>();

                foreach (var field in requireDirective.Map)
                {
                    IValueSelectionNode? selection = null;

                    if (field is not null)
                    {
                        var parser = new FieldSelectionMapParser(field);
                        selection = parser.Parse();
                    }

                    fieldsBuilder.Add(selection);
                }

                var arguments = argumentsBuilder.ToImmutable();
                var fields = fieldsBuilder.ToImmutable();

                return new FieldRequirements(context.GetSchemaName(schemaKey), declaringTypeName, arguments, fields);
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
            CompleteInputField(
                inputObjectType,
                inputObjectType.Fields.GetField(fieldDef.Name.Value, allowInaccessibleFields: true),
                fieldDef,
                context);
        }

        var directives = CompletionTools.CreateDirectiveCollection(inputObjectTypeDef.Directives, context);
        inputObjectType.Complete(new CompositeInputObjectTypeCompletionContext(
            directives,
            FeatureCollection.Empty));
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
                typeDefinition.Values.GetValue(value.Name.Value, allowInaccessibleFields: true),
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
        FusionSchemaOptions options,
        CompositeSchemaBuilderContext context)
    {
        var directives = CompletionTools.CreateDirectiveCollection(
            typeDefinitionNode.Directives,
            context,
            options.ApplySerializeAsToScalars);
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

        // if we have a @serializeAs directive we're going to set the
        // SerializationType and Pattern property.
        var type = ScalarSerializationType.Undefined;
        string? pattern = null;
        var serializeAs = typeDefinitionNode.Directives.FirstOrDefault(
            t => t.Name.Value.Equals(SerializeAs.Name));
        if (serializeAs is { Arguments: { Count: 1 } or { Count: 2 } })
        {
            var typeArg = serializeAs.Arguments.FirstOrDefault(
                t => t.Name.Value.Equals(SerializeAs.Arguments.Type));
            var patternArg = serializeAs.Arguments.FirstOrDefault(
                t => t.Name.Value.Equals(SerializeAs.Arguments.Pattern));

            switch (typeArg?.Value)
            {
                case ListValueNode typeList
                    when typeList.Items.All(t => t.Kind is SyntaxKind.EnumValue):
                    foreach (var item in typeList.Items)
                    {
                        var value = (EnumValueNode)item;
                        if (Enum.TryParse<ScalarSerializationType>(
                            value.Value,
                            ignoreCase: true,
                            out var parsedType))
                        {
                            type |= parsedType;
                        }
                    }
                    break;

                case EnumValueNode singleType
                    when Enum.TryParse<ScalarSerializationType>(
                        singleType.Value,
                        ignoreCase: true,
                        out var parsedType):
                    type = parsedType;
                    break;

                default:
                    throw new InvalidOperationException(
                        "Cannot parse the @serializeAs directive as it is missing the type argument.");
            }

            if (patternArg?.Value is StringValueNode patterValue)
            {
                pattern = patterValue.Value;
            }
        }

        typeDefinition.Complete(
            new CompositeScalarTypeCompletionContext(
                default,
                directives,
                specifiedBy,
                type,
                pattern));
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

    private static ImmutableDictionary<string, SourceSchemaInfo> CreateSourceSchemaLookup(DocumentNode schema)
    {
        var sourceSchemaDefinition =
            schema.Definitions
                .OfType<EnumTypeDefinitionNode>()
                .FirstOrDefault(t => t.Name.Value.Equals(FusionBuiltIns.Schema, StringComparison.Ordinal));

        if (sourceSchemaDefinition is null)
        {
            throw new InvalidOperationException(
                $"An executable schema must specify the `{FusionBuiltIns.Schema}` type.");
        }

        return sourceSchemaDefinition.Values
            .Select(sourceSchema => new SourceSchemaInfo(sourceSchema.Name.Value, GetSchemaName(sourceSchema)))
            .ToImmutableDictionary(sourceSchemaInfo => sourceSchemaInfo.Key);

        static string GetSchemaName(EnumValueDefinitionNode sourceSchema)
        {
            var directive = sourceSchema.Directives.FirstOrDefault(t =>
                t.Name.Value.Equals(FusionBuiltIns.SchemaMetadata, StringComparison.Ordinal));
            var nameArg = directive?.Arguments.FirstOrDefault(t => t.Name.Value.Equals("name"));
            return nameArg?.Value is StringValueNode nameValue ? nameValue.Value : sourceSchema.Name.Value;
        }
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;

        public static EmptyServiceProvider Instance { get; } = new();
    }
}
