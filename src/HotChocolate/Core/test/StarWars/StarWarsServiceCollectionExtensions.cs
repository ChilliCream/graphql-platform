using HotChocolate.StarWars;
using HotChocolate.StarWars.Data;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class StarWarsServiceCollectionExtensions
    {
        public static IServiceCollection AddStarWarsRepositories(
            this IServiceCollection services)
        {
            services.AddSingleton<CharacterRepository>();
            services.AddSingleton<ReviewRepository>();
            return services;
        }
    }
}
