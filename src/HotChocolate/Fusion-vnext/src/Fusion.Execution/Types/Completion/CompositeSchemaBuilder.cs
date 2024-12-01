using System.Collections.Frozen;
using System.Collections.Immutable;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Directives;
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

        foreach (var definition in schema.Definitions)
        {
            if (definition is INamedSyntaxNode namedSyntaxNode
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

                case ScalarTypeDefinitionNode scalarType:
                    types.Add(CreateScalarType(scalarType));
                    typeDefinitions.Add(scalarType.Name.Value, scalarType);
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
            typeDefinitions.ToImmutable());
    }

    private static CompositeObjectType CreateObjectType(
        ObjectTypeDefinitionNode definition)
    {
        return new CompositeObjectType(
            definition.Name.Value,
            definition.Description?.Value,
            CreateObjectFields(definition.Fields));
    }

    private static CompositeOutputFieldCollection CreateObjectFields(
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

    private static CompositeSchema CompleteTypes(CompositeSchemaContext schemaContext)
    {
        foreach (var type in schemaContext.Types)
        {
            switch (type)
            {
                case CompositeObjectType objectType:
                    CompleteObjectType(
                        objectType,
                        schemaContext.GetTypeDefinition<ObjectTypeDefinitionNode>(objectType.Name),
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

        var directives = CompletionTools.CreateDirectiveCollection(schemaContext.Directives, schemaContext);

        return new CompositeSchema(
            null,
            schemaContext.GetType<CompositeObjectType>(schemaContext.QueryType),
            schemaContext.MutationType is not null
                ? schemaContext.GetType<CompositeObjectType>(schemaContext.MutationType)
                : null,
            schemaContext.SubscriptionType is not null
                ? schemaContext.GetType<CompositeObjectType>(schemaContext.SubscriptionType)
                : null,
            schemaContext.Types.ToFrozenDictionary(t => t.Name),
            directives,
            schemaContext.DirectiveDefinitions.ToFrozenDictionary(t => t.Name));
    }

    private static void CompleteObjectType(
        CompositeObjectType type,
        ObjectTypeDefinitionNode typeDef,
        CompositeSchemaContext schemaContext)
    {
        foreach (var fieldDef in typeDef.Fields)
        {
            CompleteObjectField(type, type.Fields[fieldDef.Name.Value], fieldDef, schemaContext);
        }

        var directives = CompletionTools.CreateDirectiveCollection(typeDef.Directives, schemaContext);
        var interfaces = CompletionTools.CreateInterfaceTypeCollection(typeDef.Interfaces, schemaContext);
        var sources = CompletionTools.CreateSourceObjectTypeCollection(typeDef, schemaContext);
        type.Complete(new CompositeObjectTypeCompletionContext(directives, interfaces, sources));
    }

    private static void CompleteObjectField(
        CompositeObjectType declaringType,
        CompositeOutputField field,
        FieldDefinitionNode fieldDef,
        CompositeSchemaContext compositeSchemaContext)
    {
        foreach (var argumentDef in fieldDef.Arguments)
        {
            CompleteOutputFieldArguments(field.Arguments[argumentDef.Name.Value], argumentDef, compositeSchemaContext);
        }

        var directives = CompletionTools.CreateDirectiveCollection(fieldDef.Directives, compositeSchemaContext);
        var type = compositeSchemaContext.GetType(fieldDef.Type);
        var sources = BuildSourceObjectFieldCollection(field, fieldDef, compositeSchemaContext);
        field.Complete(new CompositeObjectFieldCompletionContext(declaringType, directives, type, sources));
    }

    private static SourceObjectFieldCollection BuildSourceObjectFieldCollection(
        CompositeOutputField field,
        FieldDefinitionNode fieldDef,
        CompositeSchemaContext compositeSchemaContext)
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
                    CompleteType(fieldDef.Type, fieldDirective.SourceType, compositeSchemaContext)));
        }

        return new SourceObjectFieldCollection(temp.ToImmutable());

        static FieldRequirements? ParseRequirements(
            ImmutableArray<RequireDirective> requireDirectives,
            string schemaName)
        {
            var requireDirective = requireDirectives.FirstOrDefault(t => t.SchemaName == schemaName);

            if (requireDirective is not null)
            {
                var arguments = ImmutableArray.CreateBuilder<RequiredArgument>();

                foreach (var argument in requireDirective.Field.Arguments)
                {
                    arguments.Add(new RequiredArgument(argument.Name.Value, argument.Type));
                }

                var fields = ImmutableArray.CreateBuilder<FieldPath>();

                foreach (var field in requireDirective.Map)
                {
                    fields.Add(FieldPath.Parse(field));
                }

                return new FieldRequirements(schemaName, arguments.ToImmutable(), fields.ToImmutable());
            }

            return null;
        }

        static ICompositeType CompleteType(
            ITypeNode type,
            ITypeNode? sourceType,
            CompositeSchemaContext schemaContext)
        {
            if (sourceType is null)
            {
                return schemaContext.GetType(type);
            }

            return schemaContext.GetType(sourceType, type.NamedType().Name.Value);
        }
    }

    private static void CompleteOutputFieldArguments(
        CompositeInputField argument,
        InputValueDefinitionNode argumentDef,
        CompositeSchemaContext completionContext)
    {
        var directives = CompletionTools.CreateDirectiveCollection(argumentDef.Directives, completionContext);
        var type = completionContext.GetType(argumentDef.Type);
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
}
