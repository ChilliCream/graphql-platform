using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge.Rewriters
{
    internal class RenameTypeRewriter
        : ITypeRewriter
    {
        public RenameTypeRewriter(
            string originalTypeName,
            string newTypeName,
            string? schemaName = null)
        {
            OriginalTypeName = originalTypeName.EnsureNotEmpty(nameof(originalTypeName));
            NewTypeName = newTypeName.EnsureNotEmpty(nameof(newTypeName));
            SchemaName = schemaName?.EnsureNotEmpty(nameof(schemaName));
        }

        public string OriginalTypeName { get; }

        public string NewTypeName { get; }

        public string? SchemaName { get; }

        public ITypeDefinitionNode Rewrite(
            ISchemaInfo schema,
            ITypeDefinitionNode typeDefinition)
        {
            if (SchemaName.HasValue && !SchemaName.Value.Equals(schema.Name))
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
}
