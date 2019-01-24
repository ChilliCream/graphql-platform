using System;
using HotChocolate.Execution;
using HotChocolate.Stitching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotChocolate
{
    public static class StitchingServiceCollectionExtensions
    {
        public static IServiceCollection AddRemoteQueryExecutor(
            this IServiceCollection services,
            Func<RemoteExecutorBuilder, RemoteExecutorBuilder> builder)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            services.TryAddStitchingContext();
            services.AddSingleton(sp =>
                builder(RemoteExecutorBuilder.New()).Build());
            return services;
        }

        public static IServiceCollection AddRemoteQueryExecutor(
            this IServiceCollection services,
            string schemaName,
            IQueryExecutor queryExecutor)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (string.IsNullOrEmpty(schemaName))
            {
                throw new ArgumentException(
                    "The schema name mustn't be null or empty.",
                    nameof(schemaName));
            }

            if (queryExecutor == null)
            {
                throw new ArgumentNullException(nameof(queryExecutor));
            }

            services.TryAddStitchingContext();
            services.AddSingleton<IRemoteExecutorAccessor>(
                new RemoteExecutorAccessor(schemaName, queryExecutor));
            return services;
        }

        public static IServiceCollection AddRemoteQueryExecutor(
            this IServiceCollection services,
            string schemaName,
            Func<IServiceProvider, IQueryExecutor> queryExecutorFactory)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (string.IsNullOrEmpty(schemaName))
            {
                throw new ArgumentException(
                    "The schema name mustn't be null or empty.",
                    nameof(schemaName));
            }

            if (queryExecutorFactory == null)
            {
                throw new ArgumentNullException(nameof(queryExecutorFactory));
            }

            services.TryAddStitchingContext();
            services.AddSingleton<IRemoteExecutorAccessor>(sp =>
                new RemoteExecutorAccessor(
                    schemaName,
                    queryExecutorFactory(sp)));
            return services;
        }

        private static void TryAddStitchingContext(
            this IServiceCollection services)
        {
            services.TryAddSingleton<IStitchingContext>(
                s => new StitchingContext(
                    s.GetServices<IRemoteExecutorAccessor>()));
        }
    }
}
