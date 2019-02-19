using System;
using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge.Rewriters
{
    internal class RemoveRootTypeRewriter
        : IDocumentRewriter
    {
        private readonly NameString? _schemaName;

        public RemoveRootTypeRewriter()
        {
        }

        public RemoveRootTypeRewriter(NameString schemaName)
        {
            _schemaName = schemaName.EnsureNotEmpty(nameof(schemaName));
        }

        public DocumentNode Rewrite(ISchemaInfo schema, DocumentNode document)
        {
            if (_schemaName.HasValue && !_schemaName.Value.Equals(schema.Name))
            {
                return document;
            }

            var definitions = new List<IDefinitionNode>(document.Definitions);

            RemoveType(definitions, schema.QueryType);
            RemoveType(definitions, schema.QueryType);
            RemoveType(definitions, schema.QueryType);

            return document.WithDefinitions(definitions);
        }

        private static void RemoveType(
            ICollection<IDefinitionNode> definitions,
            ITypeDefinitionNode typeDefinition)
        {
            if (typeDefinition == null)
            {
                definitions.Remove(typeDefinition);
            }
        }
    }
}
