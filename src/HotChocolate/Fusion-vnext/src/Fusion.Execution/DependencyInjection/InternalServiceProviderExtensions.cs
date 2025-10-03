using HotChocolate;
using HotChocolate.Utilities;

namespace Microsoft.Extensions.DependencyInjection;

internal static class InternalServiceProviderExtensions
{
    /// <summary>
    /// Gets a service provided that represents the combined services from the schema services
    /// and application services.
    /// </summary>
    /// <param name="services">
    /// The schema services.
    /// </param>
    /// <returns>
    /// The service.
    /// </returns>
    public static IServiceProvider GetCombinedServices(this IServiceProvider services)
        => services as CombinedServiceProvider ??
            new CombinedServiceProvider(
                services.GetRootServiceProvider(),
                services);

    /// <summary>
    /// Gets the root service provider from the schema services. This allows
    /// schema services to access application level services.
    /// </summary>
    /// <param name="services">
    /// The schema services.
    /// </param>
    /// <returns>
    /// The root service provider.
    /// </returns>
    public static IServiceProvider GetRootServiceProvider(this IServiceProvider services)
        => services.GetRequiredService<IRootServiceProviderAccessor>().ServiceProvider;

    public static T GetRequiredRootService<T>(this IServiceProvider services) where T : notnull
        => services.GetRequiredService<IRootServiceProviderAccessor>().ServiceProvider.GetRequiredService<T>();
}
