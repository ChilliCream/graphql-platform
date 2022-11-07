using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge.Rewriters
{
    internal class RemoveTypeRewriter : IDocumentRewriter
    {
        public RemoveTypeRewriter(string typeName, string? schemaName = null)
        {
            TypeName = typeName.EnsureNotEmpty(nameof(typeName));
            SchemaName = schemaName?.EnsureNotEmpty(nameof(schemaName));
        }

        public string TypeName { get; }

        public string? SchemaName { get; }

        public DocumentNode Rewrite(ISchemaInfo schema, DocumentNode document)
        {
            if (SchemaName.HasValue && !SchemaName.Value.Equals(schema.Name))
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
}
