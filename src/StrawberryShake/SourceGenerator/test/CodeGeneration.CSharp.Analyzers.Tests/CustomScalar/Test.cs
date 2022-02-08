using Microsoft.Extensions.DependencyInjection;

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers.CustomScalar;

public static class DependencyInjection
{
    public static void Configure(IServiceCollection services)
    {
        services.AddCustomScalarClient();
    }
}