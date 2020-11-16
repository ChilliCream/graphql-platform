using System;
using System.Diagnostics.CodeAnalysis;

#nullable enable

namespace HotChocolate.Utilities
{
    public static class ServiceProviderExtensions
    {
        public static IServiceProvider Include(
            this IServiceProvider first,
            IServiceProvider second) =>
            new CombinedServiceProvider(first, second);

        [return: MaybeNull]
        public static T GetOrCreateService<T>(this IServiceProvider services, Type type)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (services.GetService(type) is T s)
            {
                return s;
            }

            return CreateInstance<T>(services, type);
        }

        public static bool TryGetOrCreateService<T>(
            this IServiceProvider services,
            Type type,
            [NotNullWhen(true)] out T service)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (services.GetService(type) is T s)
            {
                service = s;
                return true;
            }

            return TryCreateInstance<T>(services, type, out service);
        }

        [return: MaybeNull]
        public static T CreateInstance<T>(this IServiceProvider services, Type type)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var factory = new ServiceFactory { Services = services };
            if (factory.CreateInstance(type) is T casted)
            {
                return casted;
            }
            return default;
        }

        public static bool TryCreateInstance<T>(
            this IServiceProvider services,
            Type type,
            [NotNullWhen(true)] out T service)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var factory = new ServiceFactory { Services = services };
            if (factory.CreateInstance(type) is T casted)
            {
                service = casted;
                return true;
            }

            service = default!;
            return false;
        }
    }
}
