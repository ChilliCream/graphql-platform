using System.Collections.Generic;
using HotChocolate.CodeGeneration.EntityFramework.Types;
using HotChocolate.CodeGeneration.Types;
using HotChocolate.Language;

namespace HotChocolate.CodeGeneration.EntityFramework
{
    public static class EntityFrameworkSchemaHelper
    {
        public static ISchema CreateSchema(IEnumerable<DocumentNode> documents)
        {
            var schemaBuilder = new SchemaBuilder();

            // Core
            schemaBuilder.AddDirectiveType<FilterableDirectiveType>();
            schemaBuilder.AddDirectiveType<FilteringDirectiveType>();
            schemaBuilder.AddDirectiveType<SortableDirectiveType>();
            schemaBuilder.AddDirectiveType<SortingDirectiveType>();
            schemaBuilder.AddDirectiveType<OperationDirectiveType>();
            schemaBuilder.AddDirectiveType<PagingDirectiveType>();
            schemaBuilder.AddDirectiveType<TypeNameDirectiveType>();

            // Conventions
            schemaBuilder.AddDirectiveType<SchemaConventionsDirectiveType>();

            // Relational
            schemaBuilder.AddDirectiveType<TableDirectiveType>();
            schemaBuilder.AddDirectiveType<JsonDirectiveType>();
            schemaBuilder.AddDirectiveType<KeyDirectiveType>();
            schemaBuilder.AddDirectiveType<ForeignKeyDirectiveType>();
            schemaBuilder.AddDirectiveType<IndexDirectiveType>();
            schemaBuilder.AddDirectiveType<OneToOneDirectiveType>();
            schemaBuilder.AddDirectiveType<OneToManyDirectiveType>();
            schemaBuilder.AddDirectiveType<ManyToManyDirectiveType>();

            // EntityFramework

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
