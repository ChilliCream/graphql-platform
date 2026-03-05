using Microsoft.Extensions.DependencyInjection;

namespace Mocha;

/// <summary>
/// Provides extension methods for resolving the application-level service provider from a
/// bus-scoped service provider.
/// </summary>
public static class RootServiceProviderAccessorExtensions
{
    /// <summary>
    /// Resolves the root application-level <see cref="IServiceProvider"/> from the bus-scoped
    /// service provider via <see cref="IRootServiceProviderAccessor"/>.
    /// </summary>
    /// <param name="sp">The bus-scoped service provider.</param>
    /// <returns>The root application-level service provider.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no <see cref="IRootServiceProviderAccessor"/> is registered.
    /// </exception>
    public static IServiceProvider GetApplicationServices(this IServiceProvider sp)
    {
        return sp.GetService<IRootServiceProviderAccessor>()?.ServiceProvider
            ?? throw new InvalidOperationException("No root services found");
    }
}
