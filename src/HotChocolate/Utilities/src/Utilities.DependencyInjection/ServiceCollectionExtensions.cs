using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Utilities;

internal static class ServiceCollectionExtensions
{
    public static bool IsImplementationTypeRegistered<TService>(
        this IServiceCollection services)
    {
        return services.Any(t => !t.IsKeyedService && t.ImplementationType == typeof(TService));
    }

    public static bool IsServiceTypeRegistered<TService>(
        this IServiceCollection services)
    {
        return services.Any(t => !t.IsKeyedService && t.ServiceType == typeof(TService));
    }

    public static IServiceCollection RemoveService<TService>(
        this IServiceCollection services)
    {
        var serviceDescriptor = services.FirstOrDefault(t => !t.IsKeyedService && t.ServiceType == typeof(TService));

        if (serviceDescriptor is not null)
        {
            services.Remove(serviceDescriptor);
        }

        return services;
    }
}
