using System;

namespace HotChocolate.Utilities
{
    internal static class ServiceProviderExtensions
    {
        public static ITypeConversion GetTypeConversion(
            this IServiceProvider services)
        {
            return GetServiceOrDefault(services, TypeConversion.Default);
        }

        public static T GetServiceOrDefault<T>(
            this IServiceProvider services,
            T defaultService)
        {
            object service = services?.GetService(typeof(T));
            if (service == null)
            {
                return defaultService;
            }
            return (T)service;
        }
    }
}
