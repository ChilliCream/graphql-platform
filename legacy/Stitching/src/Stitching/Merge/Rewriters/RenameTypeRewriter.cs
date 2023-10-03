using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Stitching.Merge.Rewriters;

internal class RenameTypeRewriter
    : ITypeRewriter
{
    public RenameTypeRewriter(
        string originalTypeName,
        string newTypeName,
        string? schemaName = null)
    {
        OriginalTypeName = originalTypeName.EnsureGraphQLName(nameof(originalTypeName));
        NewTypeName = newTypeName.EnsureGraphQLName(nameof(newTypeName));
        SchemaName = schemaName?.EnsureGraphQLName(nameof(schemaName));
    }

    public string OriginalTypeName { get; }

    public string NewTypeName { get; }

    public string? SchemaName { get; }

    public ITypeDefinitionNode Rewrite(
        ISchemaInfo schema,
        ITypeDefinitionNode typeDefinition)
    {
        if (!string.IsNullOrEmpty(SchemaName) && !SchemaName.Equals(schema.Name))
        {
            return typeDefinition;
        }

        if (!OriginalTypeName.Equals(typeDefinition.Name.Value))
        {
            return typeDefinition;
        }

        return typeDefinition.Rename(NewTypeName, schema.Name);
    }
}
