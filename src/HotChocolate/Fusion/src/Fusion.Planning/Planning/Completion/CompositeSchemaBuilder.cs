using System.Collections.Immutable;
using HotChocolate.Fusion.Planning.Collections;
using HotChocolate.Fusion.Planning.Directives;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Completion;

public class CompositeSchemaBuilder
{
    public void Test(DocumentNode documentNode)
    {
        var context = CreateTypes(documentNode);
        CompleteTypes(context);
    }

    private CompositeSchemaContext CreateTypes(DocumentNode schema)
    {
        var types = ImmutableArray.CreateBuilder<ICompositeNamedType>();
        var typeDefinitions = ImmutableDictionary.CreateBuilder<string, ITypeDefinitionNode>();

        foreach (var definition in schema.Definitions)
        {
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
            }
        }

        return new CompositeSchemaContext(types.ToImmutable(), typeDefinitions.ToImmutable());
    }

    private static CompositeObjectType CreateObjectType(
        ObjectTypeDefinitionNode definition)
    {
        return new CompositeObjectType(
            definition.Name.Value,
            definition.Description?.Value,
            CreateObjectFields(definition.Fields));
    }

    private static CompositeObjectFieldCollection CreateObjectFields(
        IReadOnlyList<FieldDefinitionNode> fields)
    {
        var sourceFields = new CompositeObjectField[fields.Count];

        for (var i = 0; i < fields.Count; i++)
        {
            var field = fields[i];
            var isDeprecated = DeprecatedDirectiveParser.TryParse(field.Directives, out var deprecated);

            sourceFields[i] = new CompositeObjectField(
                field.Name.Value,
                field.Description?.Value,
                isDeprecated,
                deprecated?.Reason,
                CreateOutputFieldArguments(field.Arguments));
        }

        return new CompositeObjectFieldCollection(sourceFields);
    }

    private static CompositeInputFieldCollection CreateOutputFieldArguments(
        IReadOnlyList<InputValueDefinitionNode> arguments)
    {
        if(arguments.Count == 0)
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

    private static void CompleteTypes(CompositeSchemaContext schemaContext)
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
    }

    private static void CompleteObjectType(
        CompositeObjectType type,
        ObjectTypeDefinitionNode typeDef,
        CompositeSchemaContext schemaContext)
    {
        foreach (var fieldDef in typeDef.Fields)
        {
            CompleteObjectField(type.Fields[fieldDef.Name.Value], fieldDef, schemaContext);
        }

        var directives = CompletionTools.CreateDirectiveCollection(typeDef.Directives, schemaContext);
        var interfaces = CompletionTools.CreateInterfaceTypeCollection(typeDef.Interfaces, schemaContext);
        type.Complete(new CompositeObjectTypeCompletionContext(directives, interfaces));
    }

    private static void CompleteObjectField(
        CompositeObjectField field,
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
        field.Complete(new CompositeObjectFieldCompletionContext(directives, type, sources));
    }

    private static SourceObjectFieldCollection BuildSourceObjectFieldCollection(
        CompositeObjectField field,
        FieldDefinitionNode fieldDef,
        CompositeSchemaContext compositeSchemaContext)
    {
        var fieldDirectives = FieldDirectiveParser.Parse(fieldDef.Directives);
        var requireDirectives = RequiredDirectiveParser.Parse(fieldDef.Directives);
        var temp = ImmutableArray.CreateBuilder<SourceObjectField>();

        foreach (var fieldDirective in fieldDirectives)
        {
            temp.Add(
                new SourceObjectField(
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

                var fields = ImmutableArray.CreateBuilder<RequiredField>();

                foreach (var field in requireDirective.Map)
                {
                    fields.Add(RequiredField.Parse(field));
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
