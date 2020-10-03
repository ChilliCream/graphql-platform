using System;
using System.Linq;
using HotChocolate.Configuration;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Stitching;
using HotChocolate.Stitching.Client;
using HotChocolate.Stitching.Delegation;
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
            IRemoteExecutorAccessor executorAccessor)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (executorAccessor == null)
            {
                throw new ArgumentNullException(nameof(executorAccessor));
            }

            services.TryAddStitchingContext();
            services.AddSingleton<IRemoteExecutorAccessor>(executorAccessor);
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
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.TryAddScoped<IStitchingContext>(
                s => new StitchingContext(
                    s,
                    s.GetServices<IRemoteExecutorAccessor>()));

            if (!services.Any(d =>
                d.ImplementationType == typeof(RemoteQueryBatchOperation)))
            {
                services.AddScoped<
                    IBatchOperation,
                    RemoteQueryBatchOperation>();
            }
        }

        public static IServiceCollection AddStitchedSchema(
            this IServiceCollection services,
            string schema)
        {
            return AddStitchedSchema(services, schema, c => { });
        }


        public static IServiceCollection AddStitchedSchema(
            this IServiceCollection services,
            string schema,
            Action<ISchemaConfiguration> configure)
        {
            return AddStitchedSchema(services, schema, configure,
                new QueryExecutionOptions());
        }

        public static IServiceCollection AddStitchedSchema(
            this IServiceCollection services,
            string schema,
            IQueryExecutionOptionsAccessor options)
        {
            return AddStitchedSchema(services, schema, c => { }, options);
        }

        public static IServiceCollection AddStitchedSchema(
            this IServiceCollection services,
            string schema,
            Action<ISchemaConfiguration> configure,
            IQueryExecutionOptionsAccessor options)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (string.IsNullOrEmpty(schema))
            {
                throw new ArgumentException(
                    "The schema mustn't be null or empty.",
                    nameof(schema));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            services.TryAddSingleton<
                IQueryResultSerializer,
                JsonQueryResultSerializer>();

            return services.AddSingleton(sp =>
            {
                return Schema.Create(
                    schema,
                    c =>
                    {
                        configure(c);
                        c.UseSchemaStitching();
                        c.RegisterServiceProvider(sp);
                    })
                    .MakeExecutable(b => b.UseStitchingPipeline(options));
            })
            .AddSingleton(sp => sp.GetRequiredService<IQueryExecutor>().Schema);
        }

        public static IServiceCollection AddStitchedSchema(
            this IServiceCollection services,
            Action<IStitchingBuilder> build)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (build == null)
            {
                throw new ArgumentNullException(nameof(build));
            }

            var stitchingBuilder = new StitchingBuilder();
            build(stitchingBuilder);
            stitchingBuilder.Populate(services);
            return services;
        }

        public static IServiceCollection AddQueryDelegationRewriter<T>(
            this IServiceCollection services)
            where T : class, IQueryDelegationRewriter

        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            return services.AddScoped<IQueryDelegationRewriter, T>();
        }
    }
}
