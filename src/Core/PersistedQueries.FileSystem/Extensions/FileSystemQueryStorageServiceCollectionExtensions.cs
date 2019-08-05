using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using HotChocolate.Execution;
using HotChocolate.PersistedQueries.FileSystem;
using System;

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

            services.AddReadOnlyFileSystemQueryStorage(cacheDirectory);
            services.AddSingleton<IWriteStoredQueries>(sp =>
                sp.GetRequiredService<FileSystemQueryStorage>());
            return services;
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

            services.AddReadOnlyFileSystemQueryStorage();
            services.AddSingleton<IWriteStoredQueries>(sp =>
                sp.GetRequiredService<FileSystemQueryStorage>());
            return services;
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

            services.AddSingleton<FileSystemQueryStorage>();
            services.AddSingleton<IReadStoredQueries>(sp =>
                sp.GetRequiredService<FileSystemQueryStorage>());
            services.RemoveService<IQueryFileMap>();
            services.AddSingleton<IQueryFileMap>(
                new DefaultQueryFileMap(cacheDirectory));
            return services;
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

            services.AddSingleton<FileSystemQueryStorage>();
            services.AddSingleton<IReadStoredQueries>(sp =>
                sp.GetRequiredService<FileSystemQueryStorage>());
            services.TryAddSingleton<IQueryFileMap, DefaultQueryFileMap>();
            return services;
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
