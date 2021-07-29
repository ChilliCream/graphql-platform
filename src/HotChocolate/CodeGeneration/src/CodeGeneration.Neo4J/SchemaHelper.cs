using System.Collections.Generic;
using HotChocolate.CodeGeneration.Types;
using HotChocolate.Language;

namespace HotChocolate.CodeGeneration.Neo4J.Types
{
    public static class SchemaHelper
    {
        public static ISchema CreateSchema(IEnumerable<DocumentNode> documents)
        {
            var schemaBuilder = new SchemaBuilder();

            schemaBuilder.AddDirectiveType<FilterableDirectiveType>();
            schemaBuilder.AddDirectiveType<FilteringDirectiveType>();
            schemaBuilder.AddDirectiveType<SortableDirectiveType>();
            schemaBuilder.AddDirectiveType<SortingDirectiveType>();
            schemaBuilder.AddDirectiveType<OperationDirectiveType>();
            schemaBuilder.AddDirectiveType<PagingDirectiveType>();
            schemaBuilder.AddDirectiveType<TypeNameDirectiveType>();

            schemaBuilder.AddDirectiveType<RelationshipDirectiveType>();

            schemaBuilder.ModifyOptions(o => o.StrictValidation = false);
            schemaBuilder.Use(next => next);

            foreach (DocumentNode? document in documents)
            {
                schemaBuilder.AddDocument(document);
            }

            return schemaBuilder.Create();
        }
    }
}
