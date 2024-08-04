using System.Collections.Immutable;
using HotChocolate.Fusion.Planning.Collections;
using HotChocolate.Fusion.Planning.Directives;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning.Completion;

public class CompositeSchemaBuilder
{
    private Dictionary<string, ITypeDefinitionNode> _typeDefinitionNodes = new();

    public void Test(DocumentNode documentNode)
    {
        var context = CreateTypes(documentNode);
    }

    private TypeContext CreateTypes(DocumentNode schema)
    {
        var types = ImmutableArray.CreateBuilder<ICompositeType>();
        var typeDefinitions = ImmutableDictionary.CreateBuilder<string, ITypeDefinitionNode>();

        foreach (var definition in schema.Definitions)
        {
            switch (definition)
            {
                case ObjectTypeDefinitionNode objectType:
                    types.Add(CreateObjectType(objectType));
                    typeDefinitions.Add(objectType.Name.Value, objectType);
                    break;
            }
        }

        return new TypeContext(types.ToImmutable(), typeDefinitions.ToImmutable());
    }

    private static CompositeObjectType CreateObjectType(ObjectTypeDefinitionNode definition)
    {
        return new CompositeObjectType(
            definition.Name.Value,
            definition.Description?.Value,
            CreateObjectFields(definition.Fields));
    }

    private static CompositeObjectFieldCollection CreateObjectFields(IReadOnlyList<FieldDefinitionNode> fields)
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


    private void CompleteObjectType(CompositeObjectType type, ObjectTypeDefinitionNode typeDefinition)
    {

    }

    private void Complete(CompositeObjectField field, FieldDefinitionNode fieldDefinition)
    {
    }

    private sealed class TypeContext(
        ImmutableArray<ICompositeType> types,
        ImmutableDictionary<string, ITypeDefinitionNode> typeDefinitions)
    {
        public ImmutableArray<ICompositeType> Types { get; } = types;

        public T GetTypeDefinition<T>(string typeName)
            where T : ITypeDefinitionNode
        {
            if (typeDefinitions.TryGetValue(typeName, out var typeDefinition))
            {
                return (T)typeDefinition;
            }

            throw new InvalidOperationException();
        }
    }
}
