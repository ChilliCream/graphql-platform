using HotChocolate.Execution;
using HotChocolate.PersistedOperations.Redis;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace HotChocolate;

/// <summary>
/// Provides utility methods to setup dependency injection.
/// </summary>
public static class HotChocolateRedisPersistedOperationsServiceCollectionExtensions
{
    /// <summary>
    /// Adds a Redis-based operation document storage to the service collection.
    /// </summary>
    /// <param name="services">
    /// The service collection to which the services are added.
    /// </param>
    /// <param name="databaseFactory">
    /// A factory that resolves the redis database that
    /// shall be used for persistence.
    /// </param>
    /// <param name="queryExpiration">
    /// A timeout after which an operation document is removed from the Redis cache.
    /// </param>
    public static IServiceCollection AddRedisOperationDocumentStorage(
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
            .RemoveService<IOperationDocumentStorage>()
            .AddSingleton<IOperationDocumentStorage>(
                sp => new RedisOperationDocumentStorage(databaseFactory(sp), queryExpiration));
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
