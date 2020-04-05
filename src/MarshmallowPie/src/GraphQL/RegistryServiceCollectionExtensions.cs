using HotChocolate;
using MarshmallowPie.GraphQL;
using MarshmallowPie.GraphQL.Environments;
using MarshmallowPie.GraphQL.ErrorFilters;
using MarshmallowPie.GraphQL.Schemas;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RegistryServiceCollectionExtensions
    {
        public static IServiceCollection AddSchemaRegistryDataLoader(
            this IServiceCollection serviceCollection)
        {
            return serviceCollection
                .AddDataLoader<EnvironmentByIdDataLoader>()
                .AddDataLoader<SchemaByIdDataLoader>()
                .AddDataLoader<SchemaByNameDataLoader>();
        }

        public static IServiceCollection AddSchemRegistryErrorFilters(
            this IServiceCollection serviceCollection)
        {
            return serviceCollection.AddErrorFilter<DuplicateKeyErrorFilter>();
        }
    }
}
