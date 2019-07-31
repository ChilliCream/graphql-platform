using System.Linq;
using HotChocolate.PersistedQueries;
using HotChocolate.PersistedQueries.FileSystem;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotChocolate
{
    /// <summary>
    /// Provides utility methods to setup dependency injection.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFileSystemQueryStorage(
            this IServiceCollection services,
            string cacheDirectory)
        {
            services.AddReadOnlyFileSystemQueryStorage(cacheDirectory);
            services.AddSingleton<IWriteStoredQueries>(sp =>
                sp.GetRequiredService<FileSystemStorage>());
            return services;
        }

        public static IServiceCollection AddFileSystemQueryStorage(
            this IServiceCollection services)
        {
            services.AddReadOnlyFileSystemQueryStorage();
            services.AddSingleton<IWriteStoredQueries>(sp =>
                sp.GetRequiredService<FileSystemStorage>());
            return services;
        }

        public static IServiceCollection AddReadOnlyFileSystemQueryStorage(
            this IServiceCollection services,
            string cacheDirectory)
        {
            services.AddSingleton<FileSystemStorage>();
            services.AddSingleton<IReadStoredQueries>(sp =>
                sp.GetRequiredService<FileSystemStorage>());
            services.RemoveService<IQueryFileMap>();
            services.AddSingleton<IQueryFileMap>(
                new DefaultQueryFileMap(cacheDirectory));
            return services;
        }


        public static IServiceCollection AddReadOnlyFileSystemQueryStorage(
            this IServiceCollection services)
        {
            services.AddSingleton<FileSystemStorage>();
            services.AddSingleton<IReadStoredQueries>(sp =>
                sp.GetRequiredService<FileSystemStorage>());
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
