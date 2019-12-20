using System;
using HotChocolate;
using HotChocolate.Types;
using MarshmallowPie.GraphQL.Resolvers;
using MarshmallowPie.GraphQL.Types;

namespace MarshmallowPie.GraphQL
{
    public static class SchemaServiceCollectionExtensions
    {
        public static ISchemaBuilder AddSchemaRegistry(
            this ISchemaBuilder builder)
        {
            return builder
                .AddQueryType<QueryType>()
                .AddType<EnvironmentQueries>()
                .AddMutationType<MutationType>()
                .AddType<EnvironmentMutations>()
                .BindClrType<string, StringType>()
                .BindClrType<Guid, IdType>();
        }
    }
}
