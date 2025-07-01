using HotChocolate;

namespace Microsoft.Extensions.DependencyInjection;

internal static class InternalServiceProviderExtensions
{
    public static T GetRequiredRootService<T>(this IServiceProvider services) where T : notnull
        => services.GetRequiredService<IRootServiceProviderAccessor>().ServiceProvider.GetRequiredService<T>();
}
