using HotChocolate.Execution.Batching;
using HotChocolate.StarWars;
using HotChocolate.Subscriptions;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AspNetCore.Tests.Utilities
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
