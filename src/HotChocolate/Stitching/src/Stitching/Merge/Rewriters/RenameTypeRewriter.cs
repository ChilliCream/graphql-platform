using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge.Rewriters
{
    internal class RenameTypeRewriter
        : ITypeRewriter
    {
        public RenameTypeRewriter(
            NameString originalTypeName,
            NameString newTypeName)
        {
            OriginalTypeName = originalTypeName
                .EnsureNotEmpty(nameof(originalTypeName));
            NewTypeName = newTypeName
                .EnsureNotEmpty(nameof(newTypeName));
        }

        public RenameTypeRewriter(
            NameString schemaName,
            NameString originalTypeName,
            NameString newTypeName)
        {
            SchemaName = schemaName
                .EnsureNotEmpty(nameof(schemaName));
            OriginalTypeName = originalTypeName
                .EnsureNotEmpty(nameof(originalTypeName));
            NewTypeName = newTypeName
                .EnsureNotEmpty(nameof(newTypeName));
        }

        public NameString? SchemaName { get; }

        public NameString OriginalTypeName { get; }

        public NameString NewTypeName { get; }

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
