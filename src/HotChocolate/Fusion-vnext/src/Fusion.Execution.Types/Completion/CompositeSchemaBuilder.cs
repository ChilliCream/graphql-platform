using System.Collections.Frozen;
using System.Collections.Immutable;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Directives;
using HotChocolate.Fusion.Utilities;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Types.Completion;

public static class CompositeSchemaBuilder
{
    public static CompositeSchema Create(DocumentNode documentNode)
    {
        var context = CreateTypes(documentNode);
        return CompleteTypes(context);
    }

    private static CompositeSchemaContext CreateTypes(DocumentNode schema)
    {
        string? queryType = null;
        string? mutationType = null;
        string? subscriptionType = null;
        IReadOnlyList<DirectiveNode> directives = Array.Empty<DirectiveNode>();
        var types = ImmutableArray.CreateBuilder<ICompositeNamedType>();
        var typeDefinitions = ImmutableDictionary.CreateBuilder<string, ITypeDefinitionNode>();
        var directiveTypes = ImmutableArray.CreateBuilder<FusionDirectiveDefinition>();
        var directiveDefinitions = ImmutableDictionary.CreateBuilder<string, DirectiveDefinitionNode>();

        foreach (var definition in schema.Definitions)
        {
            if (definition is IHasName namedSyntaxNode
                && (FusionTypes.IsBuiltInType(namedSyntaxNode.Name.Value)
                    || FusionTypes.IsBuiltInDirective(namedSyntaxNode.Name.Value)))
            {
                continue;
            }

            switch (definition)
            {
                case ObjectTypeDefinitionNode objectType:
                    types.Add(CreateObjectType(objectType));
                    typeDefinitions.Add(objectType.Name.Value, objectType);
                    break;

                case InterfaceTypeDefinitionNode interfaceType:
                    types.Add(CreateInterfaceType(interfaceType));
                    typeDefinitions.Add(interfaceType.Name.Value, interfaceType);
                    break;

                case ScalarTypeDefinitionNode scalarType:
                    types.Add(CreateScalarType(scalarType));
                    typeDefinitions.Add(scalarType.Name.Value, scalarType);
                    break;

                case DirectiveDefinitionNode directiveType:
                    directiveTypes.Add(CreateDirectiveType(directiveType));
                    directiveDefinitions.Add(directiveType.Name.Value, directiveType);
                    break;

                case SchemaDefinitionNode schemaDefinition:
                    directives = schemaDefinition.Directives;

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

                    break;
            }
        }

        return new CompositeSchemaContext(
            queryType ?? "Query",
            mutationType,
            subscriptionType,
            directives,
            types.ToImmutable(),
            typeDefinitions.ToImmutable(),
            directiveTypes.ToImmutable(),
            directiveDefinitions.ToImmutable());
    }

    private static FusionObjectType CreateObjectType(
        ObjectTypeDefinitionNode definition)
    {
        return new FusionObjectType(
            definition.Name.Value,
            definition.Description?.Value,
            CreateOutputFields(definition.Fields));
    }

    private static FusionInterfaceType CreateInterfaceType(
        InterfaceTypeDefinitionNode definition)
    {
        return new FusionInterfaceType(
            definition.Name.Value,
            definition.Description?.Value,
            CreateOutputFields(definition.Fields));
    }

    private static CompositeOutputFieldCollection CreateOutputFields(
        IReadOnlyList<FieldDefinitionNode> fields)
    {
        var sourceFields = new CompositeOutputField[fields.Count];

        for (var i = 0; i < fields.Count; i++)
        {
            var field = fields[i];
            var isDeprecated = DeprecatedDirectiveParser.TryParse(field.Directives, out var deprecated);

            sourceFields[i] = new CompositeOutputField(
                field.Name.Value,
                field.Description?.Value,
                isDeprecated,
                deprecated?.Reason,
                CreateOutputFieldArguments(field.Arguments));
        }

        return new CompositeOutputFieldCollection(sourceFields);
    }

    private static CompositeInputFieldCollection CreateOutputFieldArguments(
        IReadOnlyList<InputValueDefinitionNode> arguments)
    {
        if (arguments.Count == 0)
        {
            return CompositeInputFieldCollection.Empty;
        }

        var temp = new CompositeInputField[arguments.Count];

        for (var i = 0; i < arguments.Count; i++)
        {
            var argument = arguments[i];
            var isDeprecated = DeprecatedDirectiveParser.TryParse(argument.Directives, out var deprecated);

            temp[i] = new CompositeInputField(
                argument.Name.Value,
                argument.Description?.Value,
                argument.DefaultValue,
                isDeprecated,
                deprecated?.Reason);
        }

        return new CompositeInputFieldCollection(temp);
    }

    private static CompositeScalarType CreateScalarType(ScalarTypeDefinitionNode definition)
    {
        return new CompositeScalarType(
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

    private static CompositeInputFieldCollection CreateInputFields(
        IReadOnlyList<InputValueDefinitionNode> fields)
    {
        if (fields.Count == 0)
        {
            return CompositeInputFieldCollection.Empty;
        }

        var sourceFields = new CompositeInputField[fields.Count];

        for (var i = 0; i < fields.Count; i++)
        {
            var field = fields[i];
            var isDeprecated = DeprecatedDirectiveParser.TryParse(field.Directives, out var deprecated);

            sourceFields[i] = new CompositeInputField(
                field.Name.Value,
                field.Description?.Value,
                field.DefaultValue,
                isDeprecated,
                deprecated?.Reason);
        }

        return new CompositeInputFieldCollection(sourceFields);
    }

    private static CompositeSchema CompleteTypes(CompositeSchemaContext schemaContext)
    {
        foreach (var type in schemaContext.Types)
        {
            switch (type)
            {
                case FusionObjectType objectType:
                    CompleteObjectType(
                        objectType,
                        schemaContext.GetTypeDefinition<ObjectTypeDefinitionNode>(objectType.Name),
                        schemaContext);
                    break;

                case FusionInterfaceType interfaceType:
                    CompleteInterfaceType(
                        interfaceType,
                        schemaContext.GetTypeDefinition<InterfaceTypeDefinitionNode>(interfaceType.Name),
                        schemaContext);
                    break;

                case CompositeScalarType scalarType:
                    CompleteScalarType(
                        scalarType,
                        schemaContext.GetTypeDefinition<ScalarTypeDefinitionNode>(scalarType.Name),
                        schemaContext);
                    break;
            }
        }

        foreach (var directiveType in schemaContext.DirectiveTypes)
        {
            CompleteDirectiveType(
                directiveType,
                schemaContext.GetDirectiveDefinition(directiveType.Name),
                schemaContext);
        }

        var directives = CompletionTools.CreateDirectiveCollection(schemaContext.Directives, schemaContext);

        return new CompositeSchema(
            null,
            schemaContext.GetType<FusionObjectType>(schemaContext.QueryType),
            schemaContext.MutationType is not null
                ? schemaContext.GetType<FusionObjectType>(schemaContext.MutationType)
                : null,
            schemaContext.SubscriptionType is not null
                ? schemaContext.GetType<FusionObjectType>(schemaContext.SubscriptionType)
                : null,
            schemaContext.Types.ToFrozenDictionary(t => t.Name),
            directives,
            schemaContext.DirectiveTypes.ToFrozenDictionary(t => t.Name));
    }

    private static void CompleteObjectType(
        FusionObjectType type,
        ObjectTypeDefinitionNode typeDef,
        CompositeSchemaContext schemaContext)
    {
        foreach (var fieldDef in typeDef.Fields)
        {
            CompleteOutputField(type, type.Fields[fieldDef.Name.Value], fieldDef, schemaContext);
        }

        var directives = CompletionTools.CreateDirectiveCollection(typeDef.Directives, schemaContext);
        var interfaces = CompletionTools.CreateInterfaceTypeCollection(typeDef.Interfaces, schemaContext);
        var sources = CompletionTools.CreateSourceObjectTypeCollection(typeDef, schemaContext);
        type.Complete(new CompositeObjectTypeCompletionContext(directives, interfaces, sources));
    }

    private static void CompleteInterfaceType(
        FusionInterfaceType type,
        InterfaceTypeDefinitionNode typeDef,
        CompositeSchemaContext schemaContext)
    {
        foreach (var fieldDef in typeDef.Fields)
        {
            CompleteOutputField(type, type.Fields[fieldDef.Name.Value], fieldDef, schemaContext);
        }

        var directives = CompletionTools.CreateDirectiveCollection(typeDef.Directives, schemaContext);
        var interfaces = CompletionTools.CreateInterfaceTypeCollection(typeDef.Interfaces, schemaContext);
        var sources = CompletionTools.CreateSourceInterfaceTypeCollection(typeDef, schemaContext);
        type.Complete(new CompositeInterfaceTypeCompletionContext(directives, interfaces, sources));
    }

    private static void CompleteOutputField(
        FusionComplexType declaringType,
        CompositeOutputField field,
        FieldDefinitionNode fieldDef,
        CompositeSchemaContext schemaContext)
    {
        foreach (var argumentDef in fieldDef.Arguments)
        {
            CompleteInputField(field.Arguments[argumentDef.Name.Value], argumentDef, schemaContext);
        }

        var directives = CompletionTools.CreateDirectiveCollection(fieldDef.Directives, schemaContext);
        var type = schemaContext.GetType(fieldDef.Type);
        var sources = BuildSourceObjectFieldCollection(field, fieldDef, schemaContext);
        field.Complete(new CompositeObjectFieldCompletionContext(declaringType, directives, type, sources));
    }

    private static SourceObjectFieldCollection BuildSourceObjectFieldCollection(
        CompositeOutputField field,
        FieldDefinitionNode fieldDef,
        CompositeSchemaContext schemaContext)
    {
        var fieldDirectives = FieldDirectiveParser.Parse(fieldDef.Directives);
        var requireDirectives = RequiredDirectiveParser.Parse(fieldDef.Directives);
        var temp = ImmutableArray.CreateBuilder<SourceOutputField>();

        foreach (var fieldDirective in fieldDirectives)
        {
            temp.Add(
                new SourceOutputField(
                    fieldDirective.SourceName ?? field.Name,
                    fieldDirective.SchemaName,
                    ParseRequirements(requireDirectives, fieldDirective.SchemaName),
                    CompleteType(fieldDef.Type, fieldDirective.SourceType, schemaContext)));
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

                var fieldsBuilder = ImmutableArray.CreateBuilder<FieldPath>();

                foreach (var field in requireDirective.Map)
                {
                    fieldsBuilder.Add(FieldPath.Parse(field));
                }

                var arguments = argumentsBuilder.ToImmutable();
                var fields = fieldsBuilder.ToImmutable();
                var selectionSet = fields.ToSelectionSetNode();

                return new FieldRequirements(schemaName, arguments, fields, selectionSet);
            }

            return null;
        }

        static ICompositeType CompleteType(
            ITypeNode type,
            ITypeNode? sourceType,
            CompositeSchemaContext schemaContext)
        {
            return sourceType is null
                ? schemaContext.GetType(type)
                : schemaContext.GetType(sourceType, type.NamedType().Name.Value);
        }
    }

    private static void CompleteInputField(
        CompositeInputField argument,
        InputValueDefinitionNode argumentDef,
        CompositeSchemaContext schemaContext)
    {
        var directives = CompletionTools.CreateDirectiveCollection(argumentDef.Directives, schemaContext);
        var type = schemaContext.GetType(argumentDef.Type);
        argument.Complete(new CompositeInputFieldCompletionContext(directives, type));
    }

    private static void CompleteScalarType(
        CompositeScalarType type,
        ScalarTypeDefinitionNode typeDef,
        CompositeSchemaContext schemaContext)
    {
        var directives = CompletionTools.CreateDirectiveCollection(typeDef.Directives, schemaContext);
        type.Complete(new CompositeScalarTypeCompletionContext(directives));
    }

    private static void CompleteDirectiveType(
        FusionDirectiveDefinition definition,
        DirectiveDefinitionNode typeDef,
        CompositeSchemaContext schemaContext)
    {
        foreach (var argumentDef in typeDef.Arguments)
        {
            CompleteInputField(definition.Arguments[argumentDef.Name.Value], argumentDef, schemaContext);
        }
    }
}
