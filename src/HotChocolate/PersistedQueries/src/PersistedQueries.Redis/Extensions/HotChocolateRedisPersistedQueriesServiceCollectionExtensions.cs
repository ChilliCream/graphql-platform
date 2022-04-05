using System;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.PersistedQueries.Redis;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace HotChocolate
{
    /// <summary>
    /// Provides utility methods to setup dependency injection.
    /// </summary>
    public static class HotChocolateRedisPersistedQueriesServiceCollectionExtensions
    {
        /// <summary>
        /// Adds a redis read and write query storage to the
        /// services collection.
        /// </summary>
        /// <param name="services">
        /// The service collection to which the services are added.
        /// </param>
        /// <param name="databaseFactory">
        /// A factory that resolves the redis database that
        /// shall be used for persistence.
        /// </param>
        /// <param name="queryExpiryTimeSpan">
        /// A timespan after that a query will be removed from the cache.
        /// </param>
        public static IServiceCollection AddRedisQueryStorage(
            this IServiceCollection services,
            Func<IServiceProvider, IDatabase> databaseFactory, TimeSpan? queryExpiryTimeSpan = null)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (databaseFactory is null)
            {
                throw new ArgumentNullException(nameof(databaseFactory));
            }

            return services
                .AddReadOnlyRedisQueryStorage(databaseFactory, queryExpiryTimeSpan)
                .AddSingleton<IWriteStoredQueries>(sp => sp.GetRequiredService<RedisQueryStorage>());
        }

        /// <summary>
        /// Adds a redis read-only query storage to the services collection.
        /// </summary>
        /// <param name="services">
        /// The service collection to which the services are added.
        /// </param>
        /// <param name="databaseFactory">
        /// A factory that resolves the redis database that
        /// shall be used for persistence.
        /// </param>
        /// <param name="queryExpiryTimeSpan">
        /// A timespan after that a query will be removed from the cache.
        /// </param>
        public static IServiceCollection AddReadOnlyRedisQueryStorage(
            this IServiceCollection services,
            Func<IServiceProvider, IDatabase> databaseFactory, TimeSpan? queryExpiryTimeSpan = null)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (databaseFactory is null)
            {
                throw new ArgumentNullException(nameof(databaseFactory));
            }

            return services
                .RemoveService<IReadStoredQueries>()
                .RemoveService<IWriteStoredQueries>()
                .AddSingleton(sp => new RedisQueryStorage(databaseFactory(sp), queryExpiryTimeSpan))
                .AddSingleton<IReadStoredQueries>(sp => sp.GetRequiredService<RedisQueryStorage>());
        }

        private static IServiceCollection RemoveService<TService>(
            this IServiceCollection services)
        {
            ServiceDescriptor? serviceDescriptor = services
                .FirstOrDefault(t => t.ServiceType == typeof(TService));

            if (serviceDescriptor != null)
            {
                services.Remove(serviceDescriptor);
            }

            return services;
        }
    }
}
