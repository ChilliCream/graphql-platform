using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge.Rewriters
{
    internal class RemoveTypeRewriter
        : IDocumentRewriter
    {
        private readonly NameString? _schemaName;
        private readonly NameString _typeName;

        public RemoveTypeRewriter(NameString typeName)
        {
            _typeName = typeName.EnsureNotEmpty(nameof(typeName));
        }

        public RemoveTypeRewriter(NameString schemaName, NameString typeName)
        {
            _schemaName = schemaName.EnsureNotEmpty(nameof(schemaName));
            _typeName = typeName.EnsureNotEmpty(nameof(typeName));
        }

        public DocumentNode Rewrite(ISchemaInfo schema, DocumentNode document)
        {
            if (_schemaName.HasValue && !_schemaName.Value.Equals(schema.Name))
            {
                return document;
            }

            ITypeDefinitionNode typeDefinition = document.Definitions
                .OfType<ITypeDefinitionNode>()
                .FirstOrDefault(t =>
                    _typeName.Equals(t.GetOriginalName(schema.Name)));

            if (typeDefinition == null)
            {
                return document;
            }

            var definitions = new List<IDefinitionNode>(document.Definitions);
            definitions.Remove(typeDefinition);
            return document.WithDefinitions(definitions);
        }
    }
}
