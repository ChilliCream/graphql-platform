using HotChocolate;
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
                .AddQueryType<Query>()
                .AddType<SchemaType>();
        }
    }
}
