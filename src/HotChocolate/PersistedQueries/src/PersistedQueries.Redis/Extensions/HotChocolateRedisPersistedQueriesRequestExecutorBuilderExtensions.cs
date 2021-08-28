using System;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using HotChocolate.Execution.Configuration;
using HotChocolate;

namespace Microsoft.Extensions.DependencyInjection
{
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
        public static IRequestExecutorBuilder AddRedisQueryStorage(
            this IRequestExecutorBuilder builder,
            Func<IServiceProvider, IDatabase> databaseFactory)
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
                s => s.AddRedisQueryStorage(sp => databaseFactory(sp.GetCombinedServices())));
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
        public static IRequestExecutorBuilder AddRedisQueryStorage(
            this IRequestExecutorBuilder builder,
            Func<IServiceProvider, IConnectionMultiplexer> multiplexerFactory)
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
                s => s.AddRedisQueryStorage(
                    sp => multiplexerFactory(sp.GetCombinedServices()).GetDatabase()));
        }

        /// <summary>
        /// Adds a redis read and write query storage to the
        /// services collection and uses the first <see cref="IConnectionMultiplexer"/>
        /// registered on the application services.
        /// </summary>
        /// <param name="builder">
        /// The service collection to which the services are added.
        /// </param>
        public static IRequestExecutorBuilder AddRedisQueryStorage(
            this IRequestExecutorBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AddRedisQueryStorage(
                sp => sp.GetRequiredService<IConnectionMultiplexer>());
        }

        /// <summary>
        /// Adds a redis read-only query storage to the services collection.
        /// </summary>
        /// <param name="builder">
        /// The service collection to which the services are added.
        /// </param>
        /// <param name="databaseFactory">
        /// A factory that resolves the redis database that
        /// shall be used for persistence.
        /// </param>
        public static IRequestExecutorBuilder AddReadOnlyRedisQueryStorage(
            this IRequestExecutorBuilder builder,
            Func<IServiceProvider, IDatabase> databaseFactory)
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
                s => s.AddReadOnlyRedisQueryStorage(
                    sp => databaseFactory(sp.GetCombinedServices())));
        }

        /// <summary>
        /// Adds a redis read-only query storage to the services collection.
        /// </summary>
        /// <param name="builder">
        /// The service collection to which the services are added.
        /// </param>
        /// <param name="multiplexerFactory">
        /// A factory that resolves the redis connection multiplexer.
        /// </param>
        public static IRequestExecutorBuilder AddReadOnlyRedisQueryStorage(
            this IRequestExecutorBuilder builder,
            Func<IServiceProvider, IConnectionMultiplexer> multiplexerFactory)
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
                s => s.AddReadOnlyRedisQueryStorage(
                    sp => multiplexerFactory(sp.GetCombinedServices()).GetDatabase()));
        }

        /// <summary>
        /// Adds a redis read-only query storage to the services collection
        /// and uses the first <see cref="IConnectionMultiplexer"/>
        /// registered on the application services.
        /// </summary>
        /// <param name="builder">
        /// The service collection to which the services are added.
        /// </param>
        /// <param name="multiplexerFactory">
        /// A factory that resolves the redis connection multiplexer.
        /// </param>
        public static IRequestExecutorBuilder AddReadOnlyRedisQueryStorage(
            this IRequestExecutorBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            return builder.AddReadOnlyRedisQueryStorage(
                sp => sp.GetRequiredService<IConnectionMultiplexer>());
        }
    }
}
