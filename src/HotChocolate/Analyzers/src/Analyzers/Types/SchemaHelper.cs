using System.Collections.Generic;
using HotChocolate.Language;

namespace HotChocolate.Data.Neo4J.Analyzers.Types
{
    public static class SchemaHelper
    {
        public static ISchema CreateSchema(IEnumerable<string> sourceTexts)
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

            foreach (var sourceText in sourceTexts)
            {
                schemaBuilder.AddDocument(Utf8GraphQLParser.Parse(sourceText));
            }

            return schemaBuilder.Create();
        }
    }
}
