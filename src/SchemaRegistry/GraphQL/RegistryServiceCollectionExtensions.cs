using HotChocolate;
using MarshmallowPie.GraphQL.Environments;
using MarshmallowPie.GraphQL.Schemas;
using Microsoft.Extensions.DependencyInjection;

namespace MarshmallowPie.GraphQL
{
    public static class RegistryServiceCollectionExtensions
    {
        public static IServiceCollection AddSchemaRegistryDataLoader(
            this IServiceCollection serviceCollection)
        {
            return serviceCollection
                .AddDataLoader<EnvironmentDataLoader>()
                .AddDataLoader<SchemaByIdDataLoader>()
                .AddDataLoader<SchemaByNameDataLoader>();
        }
    }
}
