using System.Collections.Generic;
using HotChocolate.Analyzers.Types.EFCore;
using HotChocolate.Analyzers.Types.Neo4J;
using HotChocolate.Language;

namespace HotChocolate.Analyzers.Types
{
    public static class SchemaHelper
    {
        public static ISchema CreateNeo4JSchema(IEnumerable<DocumentNode> documents)
        {
            var schemaBuilder = new SchemaBuilder();

            schemaBuilder.AddDirectiveType<FilterableDirectiveType>();
            schemaBuilder.AddDirectiveType<FilteringDirectiveType>();
            schemaBuilder.AddDirectiveType<SortableDirectiveType>();
            schemaBuilder.AddDirectiveType<SortingDirectiveType>();
            schemaBuilder.AddDirectiveType<OperationDirectiveType>();
            schemaBuilder.AddDirectiveType<PagingDirectiveType>();
            schemaBuilder.AddDirectiveType<RelationshipDirectiveType>();
            schemaBuilder.AddDirectiveType<TypeNameDirectiveType>();

            schemaBuilder.ModifyOptions(o => o.StrictValidation = false);
            schemaBuilder.Use(next => next);

            foreach (DocumentNode document in documents)
            {
                schemaBuilder.AddDocument(document);
            }

            return schemaBuilder.Create();
        }

        public static ISchema CreateEFCoreSchema(IEnumerable<DocumentNode> documents)
        {
            var schemaBuilder = new SchemaBuilder();

            schemaBuilder.AddDirectiveType<KeyDirectiveType>();
            schemaBuilder.AddDirectiveType<IndexDirectiveType>();
            schemaBuilder.AddDirectiveType<OneToOneDirectiveType>();

            schemaBuilder.AddDirectiveType<FilterableDirectiveType>();
            schemaBuilder.AddDirectiveType<FilteringDirectiveType>();
            schemaBuilder.AddDirectiveType<SortableDirectiveType>();
            schemaBuilder.AddDirectiveType<SortingDirectiveType>();
            schemaBuilder.AddDirectiveType<OperationDirectiveType>();
            schemaBuilder.AddDirectiveType<PagingDirectiveType>();
            schemaBuilder.AddDirectiveType<TypeNameDirectiveType>();

            schemaBuilder.ModifyOptions(o => o.StrictValidation = false);
            schemaBuilder.Use(next => next);

            foreach (DocumentNode document in documents)
            {
                schemaBuilder.AddDocument(document);
            }

            return schemaBuilder.Create();
        }
    }
}
