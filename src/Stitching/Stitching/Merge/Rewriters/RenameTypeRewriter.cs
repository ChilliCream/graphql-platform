using System;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge.Rewriters
{
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
            _originalTypeName = originalTypeName
                .EnsureNotEmpty(nameof(originalTypeName));
            _newTypeName = newTypeName
                .EnsureNotEmpty(nameof(newTypeName));
        }

        public RenameTypeRewriter(
            NameString schemaName,
            NameString originalTypeName,
            NameString newTypeName)
        {
            _schemaName = schemaName
                .EnsureNotEmpty(nameof(schemaName));
            _originalTypeName = originalTypeName
                .EnsureNotEmpty(nameof(originalTypeName));
            _newTypeName = newTypeName
                .EnsureNotEmpty(nameof(newTypeName));
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
