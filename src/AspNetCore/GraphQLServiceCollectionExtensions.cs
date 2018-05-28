using System;
using HotChocolate.Configuration;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate
{
    public static class GraphQLServiceCollectionExtensions
    {
        public static IServiceCollection AddGraphQLSchema(
            this IServiceCollection serviceCollection,
            Schema schema)
        {
            serviceCollection.AddSingleton<Schema>(schema);
            serviceCollection.AddSingleton<OperationExecuter>();
            return serviceCollection;
        }

        public static IServiceCollection AddGraphQLSchema(
            this IServiceCollection serviceCollection,
            Action<ISchemaConfiguration> configure)
        {
            Schema schema = Schema.Create(configure);
            serviceCollection.AddSingleton<Schema>(schema);
            serviceCollection.AddSingleton<OperationExecuter>();
            return serviceCollection;
        }

        public static IServiceCollection AddGraphQLSchema(
            this IServiceCollection serviceCollection,
            string schemaSource,
            Action<ISchemaConfiguration> configure)
        {
            Schema schema = Schema.Create(schemaSource, configure);
            serviceCollection.AddSingleton<Schema>(schema);
            serviceCollection.AddSingleton<OperationExecuter>();
            return serviceCollection;
        }
    }
}
