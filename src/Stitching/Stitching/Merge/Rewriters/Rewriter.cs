using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge.Rewriters
{
    public interface ITypeRewriter
    {
        ITypeDefinitionNode Rewrite(
            ISchemaInfo schema,
            ITypeDefinitionNode typeDefinition);
    }

    internal class RenameTypeRewriter
        : ITypeRewriter
    {
        private readonly NameString? _schemaName;
        private readonly NameString _originalTypeName;
        private readonly NameString _newTypeName;

        public RenameTypeRewriter(
            NameString originalTypeName,
            NameString newTypeName)
        {
            originalTypeName.EnsureNotEmpty(nameof(originalTypeName));
            newTypeName.EnsureNotEmpty(nameof(newTypeName));
            _originalTypeName = originalTypeName;
            _newTypeName = newTypeName;
        }

        public RenameTypeRewriter(
            NameString schemaName,
            NameString originalTypeName,
            NameString newTypeName)
        {
            schemaName.EnsureNotEmpty(nameof(schemaName));
            originalTypeName.EnsureNotEmpty(nameof(originalTypeName));
            newTypeName.EnsureNotEmpty(nameof(newTypeName));
            _schemaName = schemaName;
            _originalTypeName = originalTypeName;
            _newTypeName = newTypeName;
        }

        public ITypeDefinitionNode Rewrite(
            ISchemaInfo schema,
            ITypeDefinitionNode typeDefinition)
        {
            if (_schemaName.HasValue && !_schemaName.Value.Equals(schema.Name))
            {
                return typeDefinition;
            }

            if (!_originalTypeName.Equals(typeDefinition.Name.Value))
            {
                return typeDefinition;
            }

            return typeDefinition.AddSource(_newTypeName, schema.Name);
        }
    }

    internal class RenameFieldRewriter
        : ITypeRewriter
    {
        private readonly NameString? _schemaName;
        private readonly NameString _typeName;
        private readonly NameString _originalFieldName;
        private readonly NameString _newFieldName;

        public RenameFieldRewriter(NameString schemaName, )
        {
        }

        public ITypeDefinitionNode Rewrite(
            ISchemaInfo schema,
            ITypeDefinitionNode typeDefinition)
        {
            if (_schemaName.HasValue && !_schemaName.Value.Equals(schema.Name))
            {
                return typeDefinition;
            }

            if (!_originalTypeName.Equals(typeDefinition.Name.Value))
            {
                return typeDefinition;
            }

            return typeDefinition.AddSource(_newTypeName, schema.Name);
        }
    }
}
