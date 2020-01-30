using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Merge.Rewriters
{
    internal class RemoveTypeRewriter
        : IDocumentRewriter
    {
        public RemoveTypeRewriter(NameString typeName)
        {
            TypeName = typeName.EnsureNotEmpty(nameof(typeName));
        }

        public RemoveTypeRewriter(NameString schemaName, NameString typeName)
        {
            SchemaName = schemaName.EnsureNotEmpty(nameof(schemaName));
            TypeName = typeName.EnsureNotEmpty(nameof(typeName));
        }

        public NameString? SchemaName { get; }

        public NameString TypeName { get; }

        public DocumentNode Rewrite(ISchemaInfo schema, DocumentNode document)
        {
            if (SchemaName.HasValue && !SchemaName.Value.Equals(schema.Name))
            {
                return document;
            }

            ITypeDefinitionNode typeDefinition = document.Definitions
                .OfType<ITypeDefinitionNode>()
                .FirstOrDefault(t =>
                    TypeName.Equals(t.GetOriginalName(schema.Name)));

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
