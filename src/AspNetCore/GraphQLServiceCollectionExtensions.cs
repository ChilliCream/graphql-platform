using System;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate
{
    public static class GraphQLServiceCollectionExtensions
    {
        public static IServiceCollection AddGraphQL(
            this IServiceCollection serviceCollection,
            Action<ISchemaConfiguration> configure)
        {
            serviceCollection.AddSingleton<Schema>(s => Schema.Create(c =>
            {
                c.RegisterServiceProvider(s);
                configure(c);
            }));
            serviceCollection.AddSingleton<QueryExecuter>(
                s => new QueryExecuter(s.GetRequiredService<Schema>()));
            return serviceCollection;
        }

        public static IServiceCollection AddGraphQL(
            this IServiceCollection serviceCollection,
            string schemaSource,
            Action<ISchemaConfiguration> configure)
        {
            Schema schema = Schema.Create(schemaSource, configure);
            serviceCollection.AddSingleton<Schema>(s => Schema.Create(
                schemaSource, c =>
            {
                c.RegisterServiceProvider(s);
                configure(c);
            }));
            serviceCollection.AddSingleton<QueryExecuter>(
                s => new QueryExecuter(s.GetRequiredService<Schema>()));
            return serviceCollection;
        }
    }
}
