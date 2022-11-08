using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;

namespace HotChocolate.Stitching.Merge.Rewriters;

internal class RemoveFieldRewriter
    : ITypeRewriter
{
    public RemoveFieldRewriter(FieldReference field, string? schemaName = null)
    {
        Field = field ?? throw new ArgumentNullException(nameof(field));
        SchemaName = schemaName?.EnsureGraphQLName(nameof(schemaName));
    }

    public FieldReference Field { get; }

    public string? SchemaName { get; }

    public ITypeDefinitionNode Rewrite(
        ISchemaInfo schema,
        ITypeDefinitionNode typeDefinition)
    {
        if (!string.IsNullOrEmpty(SchemaName) && !SchemaName.Equals(schema.Name))
        {
            return typeDefinition;
        }

        var typeName = typeDefinition.GetOriginalName(schema.Name);
        if (!Field.TypeName.Equals(typeName))
        {
            return typeDefinition;
        }

        switch (typeDefinition)
        {
            case InputObjectTypeDefinitionNode iotd:
                return RemoveFields(iotd);

            case ObjectTypeDefinitionNode otd:
                return RemoveFields(otd, f => otd.WithFields(f));

            case InterfaceTypeDefinitionNode itd:
                return RemoveFields(itd, f => itd.WithFields(f));

            default:
                return typeDefinition;
        }
    }

    private T RemoveFields<T>(
        T typeDefinition,
        RewriteFieldsDelegate<T> rewrite)
        where T : ComplexTypeDefinitionNodeBase, ITypeDefinitionNode
    {
        var renamedFields = new List<FieldDefinitionNode>();

        foreach (var field in typeDefinition.Fields)
        {
            if (!Field.FieldName.Equals(field.Name.Value))
            {
                renamedFields.Add(field);
            }
        }

        return rewrite(renamedFields);
    }

    private InputObjectTypeDefinitionNode RemoveFields(
        InputObjectTypeDefinitionNode typeDefinition)
    {
        var renamedFields = new List<InputValueDefinitionNode>();

        foreach (var field in typeDefinition.Fields)
        {
            if (!Field.FieldName.Equals(field.Name.Value))
            {
                renamedFields.Add(field);
            }
        }

        return typeDefinition.WithFields(renamedFields);
    }
}
