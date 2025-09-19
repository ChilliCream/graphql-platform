using Microsoft.Extensions.DependencyInjection;
using HotChocolate.StarWars.Data;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotChocolate.StarWars;

public static class StarWarsServiceCollectionExtensions
{
    public static IServiceCollection AddStarWarsRepositories(
        this IServiceCollection services)
    {
        services.TryAddSingleton<CharacterRepository>();
        services.TryAddSingleton<ReviewRepository>();
        return services;
    }

    public static IRequestExecutorBuilder AddStarWarsRepositories(
        this IRequestExecutorBuilder builder)
    {
        builder.Services.TryAddSingleton<CharacterRepository>();
        builder.Services.TryAddSingleton<ReviewRepository>();
        return builder;
    }
}
