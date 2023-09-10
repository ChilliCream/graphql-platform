using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Utilities;

namespace HotChocolate.Stitching.Merge.Rewriters;

internal class RenameFieldArgumentRewriter
    : ITypeRewriter
{
    public RenameFieldArgumentRewriter(
        FieldReference field,
        string argumentName,
        string newArgumentName,
        string? schemaName = null)
    {
        Field = field ?? throw new ArgumentNullException(nameof(field));
        ArgumentName = argumentName.EnsureGraphQLName(nameof(argumentName));
        NewArgumentName = newArgumentName.EnsureGraphQLName(nameof(newArgumentName));
        SchemaName = schemaName?.EnsureGraphQLName(nameof(schemaName));
    }

    public FieldReference Field { get; }

    public string ArgumentName { get; }

    public string NewArgumentName { get; }

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
            case ObjectTypeDefinitionNode otd:
                return SelectField(otd, schema.Name,
                    f => otd.WithFields(f));

            case InterfaceTypeDefinitionNode itd:
                return SelectField(itd, schema.Name,
                    f => itd.WithFields(f));

            default:
                return typeDefinition;
        }
    }

    private T SelectField<T>(
        T typeDefinition,
        string schemaName,
        RewriteFieldsDelegate<T> rewrite)
        where T : ComplexTypeDefinitionNodeBase, ITypeDefinitionNode
    {
        var renamedFields = new List<FieldDefinitionNode>();

        foreach (var field in typeDefinition.Fields)
        {
            if (Field.FieldName.Equals(field.Name.Value))
            {
                renamedFields.Add(RewriteArgument(schemaName, field));
            }
            else
            {
                renamedFields.Add(field);
            }
        }

        return rewrite(renamedFields);
    }

    private FieldDefinitionNode RewriteArgument(
        string schemaName,
        FieldDefinitionNode field)
    {
        var renamedArguments = new List<InputValueDefinitionNode>();

        foreach (var argument in field.Arguments)
        {
            if (ArgumentName.Equals(argument.Name.Value))
            {
                renamedArguments.Add(argument.Rename(
                    NewArgumentName, schemaName));
            }
            else
            {
                renamedArguments.Add(argument);
            }
        }

        return field.WithArguments(renamedArguments);
    }
}
