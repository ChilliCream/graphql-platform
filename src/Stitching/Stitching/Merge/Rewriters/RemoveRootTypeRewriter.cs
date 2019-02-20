using System;
using System.Collections.Generic;
using System.Linq;
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

            RemoveType(definitions, schema.QueryType?.Name.Value);
            RemoveType(definitions, schema.MutationType?.Name.Value);
            RemoveType(definitions, schema.SubscriptionType?.Name.Value);

            return document.WithDefinitions(definitions);
        }

        private static void RemoveType(
            ICollection<IDefinitionNode> definitions,
            string typeName)
        {
            if (typeName != null)
            {
                var rootType = definitions.OfType<ITypeDefinitionNode>()
                    .FirstOrDefault(t => t.Name.Value.Equals(typeName));
                if (rootType != null)
                {
                    definitions.Remove(rootType);
                }
            }
        }
    }
}
