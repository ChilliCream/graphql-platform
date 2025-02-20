using System.Collections.Immutable;
using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Directives;
using HotChocolate.Fusion.Utilities;
using HotChocolate.Language;
using HotChocolate.Types;
using static System.Runtime.InteropServices.ImmutableCollectionsMarshal;

namespace HotChocolate.Fusion.Types.Completion;

public static class CompositeSchemaBuilder
{
    public static FusionSchemaDefinition Create(DocumentNode documentNode)
    {
        var context = CreateTypes(documentNode);
        return CompleteTypes(context);
    }

    private static CompositeSchemaContext CreateTypes(DocumentNode schema)
    {
        string? queryType = null;
        string? mutationType = null;
        string? subscriptionType = null;
        var directives = ImmutableArray<DirectiveNode>.Empty;
        var types = ImmutableArray.CreateBuilder<ITypeDefinition>();
        var typeDefinitions = ImmutableDictionary.CreateBuilder<string, ITypeDefinitionNode>();
        var directiveTypes = ImmutableArray.CreateBuilder<FusionDirectiveDefinition>();
        var directiveDefinitions = ImmutableDictionary.CreateBuilder<string, DirectiveDefinitionNode>();

        foreach (var definition in schema.Definitions)
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
                    directives = [..schemaDefinition.Directives];

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

    private static FusionObjectTypeDefinition CreateObjectType(
        ObjectTypeDefinitionNode definition)
    {
        return new FusionObjectTypeDefinition(
            definition.Name.Value,
            definition.Description?.Value,
            CreateOutputFields(definition.Fields));
    }

    private static FusionInterfaceTypeDefinition CreateInterfaceType(
        InterfaceTypeDefinitionNode definition)
    {
        return new FusionInterfaceTypeDefinition(
            definition.Name.Value,
            definition.Description?.Value,
            CreateOutputFields(definition.Fields));
    }

    private static FusionOutputFieldDefinitionCollection CreateOutputFields(
        IReadOnlyList<FieldDefinitionNode> fields)
    {
        var sourceFields = new FusionOutputFieldDefinition[fields.Count];

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

    private static FusionScalarTypeDefinition CreateScalarType(ScalarTypeDefinitionNode definition)
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

    private static FusionSchemaDefinition CompleteTypes(CompositeSchemaContext schemaContext)
    {
        foreach (var type in schemaContext.TypeDefinitions)
        {
            switch (type)
            {
                case FusionObjectTypeDefinition objectType:
                    CompleteObjectType(
                        objectType,
                        schemaContext.GetTypeDefinition<ObjectTypeDefinitionNode>(objectType.Name),
                        schemaContext);
                    break;

                case FusionInterfaceTypeDefinition interfaceType:
                    CompleteInterfaceType(
                        interfaceType,
                        schemaContext.GetTypeDefinition<InterfaceTypeDefinitionNode>(interfaceType.Name),
                        schemaContext);
                    break;

                case FusionScalarTypeDefinition scalarType:
                    CompleteScalarType(
                        scalarType,
                        schemaContext.GetTypeDefinition<ScalarTypeDefinitionNode>(scalarType.Name),
                        schemaContext);
                    break;
            }
        }

        foreach (var directiveType in schemaContext.DirectiveDefinitions)
        {
            CompleteDirectiveType(
                directiveType,
                schemaContext.GetDirectiveDefinition(directiveType.Name),
                schemaContext);
        }

        var directives = CompletionTools.CreateDirectiveCollection(schemaContext.Directives, schemaContext);

        return new FusionSchemaDefinition(
            "Default",
            null,
            schemaContext.GetType<FusionObjectTypeDefinition>(schemaContext.QueryType),
            schemaContext.MutationType is not null
                ? schemaContext.GetType<FusionObjectTypeDefinition>(schemaContext.MutationType)
                : null,
            schemaContext.SubscriptionType is not null
                ? schemaContext.GetType<FusionObjectTypeDefinition>(schemaContext.SubscriptionType)
                : null,
            directives,
            new FusionTypeDefinitionCollection(AsArray(schemaContext.TypeDefinitions)!),
            new FusionDirectiveDefinitionCollection(AsArray(schemaContext.DirectiveDefinitions)!));
    }

    private static void CompleteObjectType(
        FusionObjectTypeDefinition type,
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
        FusionInterfaceTypeDefinition type,
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
        FusionComplexTypeDefinition declaringType,
        FusionOutputFieldDefinition fieldDefinition,
        FieldDefinitionNode fieldDef,
        CompositeSchemaContext schemaContext)
    {
        foreach (var argumentDef in fieldDef.Arguments)
        {
            CompleteInputField(fieldDefinition.Arguments[argumentDef.Name.Value], argumentDef, schemaContext);
        }

        var directives = CompletionTools.CreateDirectiveCollection(fieldDef.Directives, schemaContext);
        var type = schemaContext.GetType(fieldDef.Type);
        var sources = BuildSourceObjectFieldCollection(fieldDefinition, fieldDef, schemaContext);
        fieldDefinition.Complete(new CompositeObjectFieldCompletionContext(declaringType, directives, type, sources));
    }

    private static SourceObjectFieldCollection BuildSourceObjectFieldCollection(
        FusionOutputFieldDefinition fieldDefinition,
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
                    fieldDirective.SourceName ?? fieldDefinition.Name,
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

        static IType CompleteType(
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
        FusionInputFieldDefinition argument,
        InputValueDefinitionNode argumentDef,
        CompositeSchemaContext schemaContext)
    {
        var directives = CompletionTools.CreateDirectiveCollection(argumentDef.Directives, schemaContext);
        var type = schemaContext.GetType(argumentDef.Type);
        argument.Complete(new CompositeInputFieldCompletionContext(directives, type));
    }

    private static void CompleteScalarType(
        FusionScalarTypeDefinition type,
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
