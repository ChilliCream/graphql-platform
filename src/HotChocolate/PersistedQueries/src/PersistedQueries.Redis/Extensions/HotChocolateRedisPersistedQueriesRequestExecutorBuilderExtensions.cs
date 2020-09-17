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
                s => s.AddRedisQueryStorage(databaseFactory));
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
                s => s.AddReadOnlyRedisQueryStorage(databaseFactory));
        }
    }
}
