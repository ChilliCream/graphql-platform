using System;
using System.Linq;
using HotChocolate.Execution;
using HotChocolate.PersistedQueries.Redis;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace HotChocolate;

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
    /// <param name="queryExpiration">
    /// A timeout after which a query is removed from the Redis cache.
    /// </param>
    public static IServiceCollection AddRedisQueryStorage(
        this IServiceCollection services,
        Func<IServiceProvider, IDatabase> databaseFactory,
        TimeSpan? queryExpiration = null)
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
            .AddSingleton(sp => new RedisQueryStorage(databaseFactory(sp), queryExpiration))
            .AddSingleton<IReadStoredQueries>(sp => sp.GetRequiredService<RedisQueryStorage>())
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
    public static IServiceCollection AddReadOnlyRedisQueryStorage(
        this IServiceCollection services,
        Func<IServiceProvider, IDatabase> databaseFactory)
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
            .AddSingleton(sp => new RedisQueryStorage(databaseFactory(sp)))
            .AddSingleton<IReadStoredQueries>(sp => sp.GetRequiredService<RedisQueryStorage>());
    }

    private static IServiceCollection RemoveService<TService>(
        this IServiceCollection services)
    {
        var serviceDescriptor = services.FirstOrDefault(t => t.ServiceType == typeof(TService));

        if (serviceDescriptor != null)
        {
            services.Remove(serviceDescriptor);
        }

        return services;
    }
}
