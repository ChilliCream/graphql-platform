using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Utilities;

internal static class ServiceCollectionExtensions
{
    public static bool IsImplementationTypeRegistered<TService>(
        this IServiceCollection services)
    {
#if NET8_0_OR_GREATER
        return services.Any(t => !t.IsKeyedService && t.ImplementationType == typeof(TService));
#else
        return services.Any(t => t.ImplementationType == typeof(TService));
#endif
    }

    public static bool IsServiceTypeRegistered<TService>(
        this IServiceCollection services)
    {
#if NET8_0_OR_GREATER
        return services.Any(t => !t.IsKeyedService && t.ServiceType == typeof(TService));
#else
        return services.Any(t => t.ServiceType == typeof(TService));
#endif
    }

    public static IServiceCollection RemoveService<TService>(
        this IServiceCollection services)
    {
#if NET8_0_OR_GREATER
        var serviceDescriptor = services.FirstOrDefault(t => !t.IsKeyedService && t.ServiceType == typeof(TService));
#else
        var serviceDescriptor = services.FirstOrDefault(t => t.ServiceType == typeof(TService));
#endif

        if (serviceDescriptor is not null)
        {
            services.Remove(serviceDescriptor);
        }

        return services;
    }
}
