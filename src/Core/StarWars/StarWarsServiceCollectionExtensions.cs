using HotChocolate.StarWars.Data;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.StarWars
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
