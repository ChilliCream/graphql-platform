using StackExchange.Redis;
using HotChocolate.Execution.Configuration;
using HotChocolate;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides utility methods to setup dependency injection.
/// </summary>
public static class HotChocolateRedisPersistedOperationsRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Adds a Redis-based operation document storage to the service collection.
    /// </summary>
    /// <param name="builder">
    /// The service collection to which the services are added.
    /// </param>
    /// <param name="databaseFactory">
    /// A factory that resolves the Redis database that
    /// shall be used for persistence.
    /// </param>
    /// <param name="queryExpiration">
    /// A timeout after which an operation document is removed from the Redis cache.
    /// </param>
    public static IRequestExecutorBuilder AddRedisOperationDocumentStorage(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, IDatabase> databaseFactory,
        TimeSpan? queryExpiration = null)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (databaseFactory is null)
        {
            throw new ArgumentNullException(nameof(databaseFactory));
        }

        return builder.ConfigureSchemaServices(
            s => s.AddRedisOperationDocumentStorage(
                sp => databaseFactory(sp.GetCombinedServices()),
                queryExpiration));
    }

    /// <summary>
    /// Adds a Redis-based operation document storage to the service collection.
    /// </summary>
    /// <param name="builder">
    /// The service collection to which the services are added.
    /// </param>
    /// <param name="multiplexerFactory">
    /// A factory that resolves the Redis connection multiplexer.
    /// </param>
    /// <param name="queryExpiration">
    /// A timeout after which an operation document is removed from the Redis cache.
    /// </param>
    public static IRequestExecutorBuilder AddRedisOperationDocumentStorage(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, IConnectionMultiplexer> multiplexerFactory,
        TimeSpan? queryExpiration = null)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (multiplexerFactory is null)
        {
            throw new ArgumentNullException(nameof(multiplexerFactory));
        }

        return builder.ConfigureSchemaServices(
            s => s.AddRedisOperationDocumentStorage(
                sp => multiplexerFactory(sp.GetCombinedServices()).GetDatabase(),
                queryExpiration));
    }

    /// <summary>
    /// Adds a Redis-based operation document storage to the
    /// service collection and uses the first <see cref="IConnectionMultiplexer"/>
    /// registered on the application services.
    /// </summary>
    /// <param name="builder">
    /// The service collection to which the services are added.
    /// </param>
    /// <param name="queryExpiration">
    /// A timeout after which an operation document is removed from the Redis cache.
    /// </param>
    public static IRequestExecutorBuilder AddRedisOperationDocumentStorage(
        this IRequestExecutorBuilder builder,
        TimeSpan? queryExpiration = null)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.AddRedisOperationDocumentStorage(
            sp => sp.GetRequiredService<IConnectionMultiplexer>(),
            queryExpiration);
    }
}
