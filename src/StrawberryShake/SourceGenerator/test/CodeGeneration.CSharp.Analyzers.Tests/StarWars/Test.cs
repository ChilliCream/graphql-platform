using Microsoft.Extensions.DependencyInjection;

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers.StarWars;

public static class DependencyInjection
{
    public static void Configure(IServiceCollection services)
    {
        services.AddStarWarsClient();
    }
}