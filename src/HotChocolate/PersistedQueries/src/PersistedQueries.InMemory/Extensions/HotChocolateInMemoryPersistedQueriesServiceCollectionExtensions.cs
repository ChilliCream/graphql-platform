using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.PersistedQueries.FileSystem;

namespace HotChocolate
{
    /// <summary>
    /// Provides utility methods to setup dependency injection.
    /// </summary>
    public static class HotChocolateInMemoryPersistedQueriesServiceCollectionExtensions
    {
        /// <summary>
        /// Adds a file system read and write query storage to the
        /// services collection.
        /// </summary>
        /// <param name="services">
        /// The service collection to which the services are added.
        /// </param>
        /// <param name="cacheDirectory">
        /// The directory path that shall be used to store queries.
        /// </param>
        public static IServiceCollection AddInMemoryQueryStorage(
            this IServiceCollection services,
            string? cacheDirectory = null)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services
                .AddReadOnlyInMemoryQueryStorage(cacheDirectory)
                .AddSingleton<IWriteStoredQueries>(
                    sp => sp.GetRequiredService<InMemoryQueryStorage>());
        }

        /// <summary>
        /// Adds a file system read-only query storage to the
        /// services collection.
        /// </summary>
        /// <param name="services">
        /// The service collection to which the services are added.
        /// </param>
        /// <param name="cacheDirectory">
        /// The directory path that shall be used to read queries from.
        /// </param>
        public static IServiceCollection AddReadOnlyInMemoryQueryStorage(
            this IServiceCollection services,
            string? cacheDirectory = null)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services
                .RemoveService<IReadStoredQueries>()
                .RemoveService<IWriteStoredQueries>()
                .AddSingleton<InMemoryQueryStorage>()
                .AddSingleton<IReadStoredQueries>(
                    sp => sp.GetRequiredService<InMemoryQueryStorage>());
        }

        private static IServiceCollection RemoveService<TService>(
            this IServiceCollection services)
        {
            ServiceDescriptor? serviceDescriptor =
                services.FirstOrDefault(t => t.ServiceType == typeof(TService));

            if (serviceDescriptor != null)
            {
                services.Remove(serviceDescriptor);
            }

            return services;
        }
    }
}
