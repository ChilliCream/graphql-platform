using Microsoft.Extensions.DependencyInjection;
using HotChocolate;
using HotChocolate.StarWars;
using HotChocolate.Execution.Batching;
using HotChocolate.Subscriptions;

namespace StrawberryShake.Demo
{
    public static class TestingServiceCollectionExtensions
    {
        public static IServiceCollection AddStarWars(
            this IServiceCollection services)
        {
            // Add the custom services like repositories etc ...
            services.AddStarWarsRepositories();

            // Add in-memory event provider
            services.AddInMemorySubscriptionProvider();

            // Add GraphQL Services
            services.AddGraphQL(sp => SchemaBuilder.New()
                .AddServices(sp)
                .AddStarWarsTypes()
                .AddDirectiveType<ExportDirectiveType>()
                .Create());

            return services;
        }
    }
}
