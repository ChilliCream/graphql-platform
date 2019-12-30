using System;
using HotChocolate;
using HotChocolate.Types;
using MarshmallowPie.GraphQL.Environments;

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
                .AddMutationType(d => d.Name("Mutation"))
                .AddType<EnvironmentMutations>()
                .BindClrType<string, StringType>()
                .BindClrType<Guid, IdType>();
        }
    }
}
