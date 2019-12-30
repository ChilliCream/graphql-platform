using System;
using HotChocolate;
using HotChocolate.Types;
using MarshmallowPie.GraphQL.Environments;
using MarshmallowPie.GraphQL.Schemas;

namespace MarshmallowPie.GraphQL
{
    public static class SchemaServiceCollectionExtensions
    {
        public static ISchemaBuilder AddSchemaRegistry(
            this ISchemaBuilder builder)
        {
            return builder
                .AddQueryType(d => d.Name("Query"))
                .AddType<EnvironmentQueries>()
                .AddType<SchemaQueries>()
                .AddMutationType(d => d.Name("Mutation"))
                .AddType<EnvironmentMutations>()
                .AddType<SchemaMutations>()
                .AddType<SchemaExtension>()
                .AddType<SchemaVersionExtension>()
                .BindClrType<string, StringType>()
                .BindClrType<Guid, IdType>();
        }
    }
}
