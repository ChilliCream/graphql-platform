using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Stitching.Merge.Rewriters;

internal class RemoveTypeRewriter : IDocumentRewriter
{
    public RemoveTypeRewriter(string typeName, string? schemaName = null)
    {
        TypeName = typeName.EnsureGraphQLName(nameof(typeName));
        SchemaName = schemaName?.EnsureGraphQLName(nameof(schemaName));
    }

    public string TypeName { get; }

    public string? SchemaName { get; }

    public DocumentNode Rewrite(ISchemaInfo schema, DocumentNode document)
    {
        if (!string.IsNullOrEmpty(SchemaName) && !SchemaName.Equals(schema.Name))
        {
            return document;
        }

        var typeDefinition = document.Definitions
            .OfType<ITypeDefinitionNode>()
            .FirstOrDefault(t => TypeName.Equals(t.GetOriginalName(schema.Name)));

        if (typeDefinition is null)
        {
            return document;
        }

        var definitions = new List<IDefinitionNode>(document.Definitions);
        definitions.Remove(typeDefinition);
        return document.WithDefinitions(definitions);
    }
}
