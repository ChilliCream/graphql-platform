using System;
using HotChocolate.Resolvers;

namespace HotChocolate.Utilities
{
    public static class TypeConverterServiceProviderExtensions
    {
        public static ITypeConverter GetTypeConverter(
            this IServiceProvider services)
        {
            return GetServiceOrDefault<ITypeConverter>(
                services,
                DefaultTypeConverter.Default);
        }

        public static ITypeConverter GetTypeConverter(
            this IResolverContext resolverContext)
        {
            return resolverContext.Services.GetTypeConverter();
        }

        public static T GetServiceOrDefault<T>(
            this IServiceProvider services,
            T defaultService)
        {
            object service = services?.GetService(typeof(T));
            if (service is null)
            {
                return defaultService;
            }
            return (T)service;
        }
    }
}
