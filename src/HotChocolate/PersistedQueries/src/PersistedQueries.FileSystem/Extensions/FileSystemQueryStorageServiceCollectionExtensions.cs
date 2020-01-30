using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using HotChocolate.Execution;
using HotChocolate.PersistedQueries.FileSystem;

namespace HotChocolate
{
    /// <summary>
    /// Provides utility methods to setup dependency injection.
    /// </summary>
    public static class FileSystemQueryStorageServiceCollectionExtensions
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
        public static IServiceCollection AddFileSystemQueryStorage(
            this IServiceCollection services,
            string cacheDirectory)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (cacheDirectory is null)
            {
                throw new ArgumentNullException(nameof(cacheDirectory));
            }

            return services
                .AddReadOnlyFileSystemQueryStorage(cacheDirectory)
                .AddSingleton<IWriteStoredQueries>(sp =>
                    sp.GetRequiredService<FileSystemQueryStorage>());
        }

        /// <summary>
        /// Adds a file system read and write query storage to the
        /// services collection that will store queries in
        /// the current working directory.
        /// </summary>
        /// <param name="services">
        /// The service collection to which the services are added.
        /// </param>
        public static IServiceCollection AddFileSystemQueryStorage(
            this IServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services
                .AddReadOnlyFileSystemQueryStorage()
                .AddSingleton<IWriteStoredQueries>(sp =>
                    sp.GetRequiredService<FileSystemQueryStorage>());
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
        public static IServiceCollection AddReadOnlyFileSystemQueryStorage(
            this IServiceCollection services,
            string cacheDirectory)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (cacheDirectory is null)
            {
                throw new ArgumentNullException(nameof(cacheDirectory));
            }

            return services
                .RemoveService<IReadStoredQueries>()
                .RemoveService<IWriteStoredQueries>()
                .RemoveService<IQueryFileMap>()
                .AddSingleton<FileSystemQueryStorage>()
                .AddSingleton<IReadStoredQueries>(sp =>
                    sp.GetRequiredService<FileSystemQueryStorage>())
                .AddSingleton<IQueryFileMap>(
                    new DefaultQueryFileMap(cacheDirectory));
        }

        /// <summary>
        /// Adds a file system read-only query storage to the
        /// services collection that will store queries in
        /// the current working directory.
        /// </summary>
        /// <param name="services">
        /// The service collection to which the services are added.
        /// </param>
        public static IServiceCollection AddReadOnlyFileSystemQueryStorage(
            this IServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.TryAddSingleton<IQueryFileMap, DefaultQueryFileMap>();

            return services
                .RemoveService<IReadStoredQueries>()
                .RemoveService<IWriteStoredQueries>()
                .AddSingleton<FileSystemQueryStorage>()
                .AddSingleton<IReadStoredQueries>(sp =>
                    sp.GetRequiredService<FileSystemQueryStorage>());
        }

        private static IServiceCollection RemoveService<TService>(
            this IServiceCollection services)
        {
            ServiceDescriptor serviceDescriptor = services
                .FirstOrDefault(t => t.ServiceType == typeof(TService));

            if (serviceDescriptor != null)
            {
                services.Remove(serviceDescriptor);
            }

            return services;
        }
    }
}
