using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;

namespace HotChocolate.Stitching.Merge.Rewriters;

internal class RenameFieldRewriter : ITypeRewriter
{
    public RenameFieldRewriter(
        FieldReference field,
        string newFieldName,
        string? schemaName = null)
    {
        Field = field ?? throw new ArgumentNullException(nameof(field));
        NewFieldName = newFieldName.EnsureGraphQLName(nameof(newFieldName));
        SchemaName = schemaName?.EnsureGraphQLName(nameof(schemaName));
    }

    public FieldReference Field { get; }

    public string NewFieldName { get; }

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
                return RenameFields(iotd, schema.Name);

            case ObjectTypeDefinitionNode otd:
                return RenameFields(otd, schema.Name,
                    f => otd.WithFields(f));

            case InterfaceTypeDefinitionNode itd:
                return RenameFields(itd, schema.Name,
                    f => itd.WithFields(f));

            default:
                return typeDefinition;
        }
    }

    private T RenameFields<T>(
        T typeDefinition,
        string schemaName,
        RewriteFieldsDelegate<T> rewrite)
        where T : ComplexTypeDefinitionNodeBase, ITypeDefinitionNode
    {
        var renamedFields = new List<FieldDefinitionNode>();

        foreach (var field in typeDefinition.Fields)
        {
            renamedFields.Add(
                Field.FieldName.Equals(field.Name.Value)
                    ? field.Rename(NewFieldName, schemaName)
                    : field);
        }

        return rewrite(renamedFields);
    }

    private InputObjectTypeDefinitionNode RenameFields(
        InputObjectTypeDefinitionNode typeDefinition,
        string schemaName)
    {
        var renamedFields = new List<InputValueDefinitionNode>();

        foreach (var field in typeDefinition.Fields)
        {
            renamedFields.Add(
                Field.FieldName.Equals(field.Name.Value)
                    ? field.Rename(NewFieldName, schemaName)
                    : field);
        }

        return typeDefinition.WithFields(renamedFields);
    }
}
