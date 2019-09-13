using System;
using HotChocolate.Resolvers;

namespace HotChocolate.Utilities
{
    public static class TypeConversionServiceProviderExtensions
    {
        public static ITypeConversion GetTypeConversion(
            this IServiceProvider services)
        {
            return GetServiceOrDefault<ITypeConversion>(
                services, TypeConversion.Default);
        }

        public static ITypeConversion GetTypeConversion(
            this IResolverContext services)
        {
            return services.Service<IServiceProvider>().GetTypeConversion();
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
