using System;
using StackExchange.Redis;
using HotChocolate.Execution.Configuration;
using HotChocolate;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides utility methods to setup dependency injection.
/// </summary>
public static class HotChocolateRedisPersistedQueriesRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Adds a redis read and write query storage to the
    /// services collection.
    /// </summary>
    /// <param name="builder">
    /// The service collection to which the services are added.
    /// </param>
    /// <param name="databaseFactory">
    /// A factory that resolves the redis database that
    /// shall be used for persistence.
    /// </param>
    /// <param name="queryExpiration">
    /// A timeout after which a query is removed from the Redis cache.
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
    /// Adds a redis read and write query storage to the
    /// services collection.
    /// </summary>
    /// <param name="builder">
    /// The service collection to which the services are added.
    /// </param>
    /// <param name="multiplexerFactory">
    /// A factory that resolves the redis connection multiplexer.
    /// </param>
    /// <param name="queryExpiration">
    /// A timeout after which a query is removed from the Redis cache.
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
    /// Adds a redis read and write query storage to the
    /// services collection and uses the first <see cref="IConnectionMultiplexer"/>
    /// registered on the application services.
    /// </summary>
    /// <param name="builder">
    /// The service collection to which the services are added.
    /// </param>
    /// <param name="queryExpiration">
    /// A timeout after which a query is removed from the Redis cache.
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
