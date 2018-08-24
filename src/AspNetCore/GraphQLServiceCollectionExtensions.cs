using System;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate
{
    public static class GraphQLServiceCollectionExtensions
    {
        public static IServiceCollection AddGraphQL(
            this IServiceCollection serviceCollection,
            Schema schema)
        {
            serviceCollection.AddSingleton<Schema>(schema);
            return serviceCollection.AddQueryExecuter();
        }

        public static IServiceCollection AddGraphQL(
            this IServiceCollection serviceCollection,
            Func<IServiceProvider, Schema> schemaFactory)
        {
            serviceCollection.AddSingleton<Schema>(schemaFactory);
            return serviceCollection.AddQueryExecuter();
        }

        public static IServiceCollection AddGraphQL(
            this IServiceCollection serviceCollection,
            Action<ISchemaConfiguration> configure)
        {
            serviceCollection.AddSingleton<Schema>(s => Schema.Create(c =>
            {
                c.RegisterServiceProvider(s);
                configure(c);
            }));
            return serviceCollection.AddQueryExecuter();
        }

        public static IServiceCollection AddGraphQL(
            this IServiceCollection serviceCollection,
            string schemaSource,
            Action<ISchemaConfiguration> configure)
        {
            serviceCollection.AddSingleton<Schema>(s => Schema.Create(
                schemaSource, c =>
            {
                c.RegisterServiceProvider(s);
                configure(c);
            }));
            return serviceCollection.AddQueryExecuter();
        }

        private static IServiceCollection AddQueryExecuter(
            this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<QueryExecuter>(
                s => new QueryExecuter(s.GetRequiredService<Schema>()));
            return serviceCollection;
        }
    }
}
