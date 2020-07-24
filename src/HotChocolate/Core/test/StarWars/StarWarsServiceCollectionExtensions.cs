using Microsoft.Extensions.DependencyInjection;
using HotChocolate.StarWars.Data;
using HotChocolate.Execution.Configuration;

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

        public static IRequestExecutorBuilder AddStarWarsRepositories(
            this IRequestExecutorBuilder builder)
        {
            builder.Services.AddSingleton<CharacterRepository>();
            builder.Services.AddSingleton<ReviewRepository>();
            return builder;
        }
    }
}
