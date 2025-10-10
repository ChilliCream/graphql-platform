using HotChocolate.Fusion.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static partial class CoreFusionGatewayBuilderExtensions
{
    /// <summary>
    /// Resolves an instance of <typeparamref name="TService"/> from the application
    /// service provider and makes it available as a singleton through the schema
    /// service provider.
    /// </summary>
    /// <typeparam name="TService">
    /// The type of service.
    /// </typeparam>
    public static IFusionGatewayBuilder AddApplicationService<TService>(
        this IFusionGatewayBuilder builder)
        where TService : class
    {
        return builder.ConfigureSchemaServices(
            static (sp, sc) => sc.AddSingleton(sp.GetRequiredService<TService>()));
    }
}
