using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Stitching.Merge.Rewriters;

internal class RemoveRootTypeRewriter : IDocumentRewriter
{
    public RemoveRootTypeRewriter(string? schemaName = null) =>
        SchemaName = schemaName?.EnsureGraphQLName(nameof(schemaName));

    public string? SchemaName { get; }

    public DocumentNode Rewrite(ISchemaInfo schema, DocumentNode document)
    {
        if (!string.IsNullOrEmpty(SchemaName) && !SchemaName.Equals(schema.Name))
        {
            return document;
        }

        var definitions = new List<IDefinitionNode>(document.Definitions);

        RemoveType(definitions, schema.QueryType?.Name.Value);
        RemoveType(definitions, schema.MutationType?.Name.Value);
        RemoveType(definitions, schema.SubscriptionType?.Name.Value);

        return document.WithDefinitions(definitions);
    }

    private static void RemoveType(ICollection<IDefinitionNode> definitions, string? typeName)
    {
        if (typeName is not null)
        {
            var rootType = definitions
                .OfType<ITypeDefinitionNode>()
                .FirstOrDefault(t => t.Name.Value.Equals(typeName));

            if (rootType is not null)
            {
                definitions.Remove(rootType);
            }
        }
    }
}
