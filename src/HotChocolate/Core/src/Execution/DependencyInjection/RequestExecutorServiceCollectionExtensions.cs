using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using GreenDonut;
using HotChocolate.Execution;
using HotChocolate.Execution.Caching;
using HotChocolate.Execution.Configuration;
using HotChocolate.Fetching;
using HotChocolate.Language;
using HotChocolate;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RequestExecutorServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the <see cref="IRequestExecutorResolver"/> and related services
        /// to the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddGraphQLCore(this IServiceCollection services)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddOptions();

            // core services
            services
                .TryAddRequestExecutorFactoryOptionsMonitor()
                .TryAddTypeConverter()
                .TryAddDefaultCaches()
                .TryAddDefaultDocumentHashProvider()
                .TryAddDefaultBatchDispatcher()
                .TryAddRequestContextAccessor()
                .TryAddDefaultDataLoaderRegistry()
                .TryAddIdSerializer();

            // pools
            services
                .TryAddResultPool()
                .TryAddResolverTaskPool()
                .TryAddOperationContextPool();

            // global executor services
            services
                .TryAddVariableCoercion()
                .TryAddRequestExecutorResolver();

            // parser
            services.TryAddSingleton(ParserOptions.Default);

            return services;
        }

        /// <summary>
        /// Adds the <see cref="IRequestExecutorResolver"/> and related services to the
        /// <see cref="IServiceCollection"/> and configures a named <see cref="IRequestExecutor"/>.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/>.
        /// </param>
        /// <param name="schemaName">
        /// The logical name of the <see cref="ISchema"/> to configure.
        /// </param>
        /// <returns>
        /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure the executor.
        /// </returns>
        public static IRequestExecutorBuilder AddGraphQL(
            this IServiceCollection services,
            NameString schemaName = default)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            schemaName = schemaName.HasValue ? schemaName : Schema.DefaultName;

            services
                .AddGraphQLCore()
                .AddValidation(schemaName);

            return new DefaultRequestExecutorBuilder(services, schemaName)
                .Configure((sp, e) =>
                    e.OnRequestExecutorEvicted.Add(
                        // when ever we evict this schema we will clear the caches.
                        new OnRequestExecutorEvictedAction(
                            _ => sp.GetRequiredService<IPreparedOperationCache>().Clear())));
        }

        /// <summary>
        /// Adds the <see cref="IRequestExecutorResolver"/> and related services to the
        /// <see cref="IServiceCollection"/> and configures a named <see cref="IRequestExecutor"/>.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IRequestExecutorBuilder"/>.
        /// </param>
        /// <param name="schemaName">
        /// The logical name of the <see cref="ISchema"/> to configure.
        /// </param>
        /// <returns>
        /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure the executor.
        /// </returns>
        public static IRequestExecutorBuilder AddGraphQL(
            this IRequestExecutorBuilder builder,
            NameString schemaName = default)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            schemaName = schemaName.HasValue ? schemaName : Schema.DefaultName;

            builder.Services.AddValidation(schemaName);

            return new DefaultRequestExecutorBuilder(builder.Services, schemaName);
        }

        public static IServiceCollection AddDocumentCache(
            this IServiceCollection services,
            int capacity = 100)
        {
            services.RemoveAll<IDocumentCache>();
            services.AddSingleton<IDocumentCache>(
                sp => new DefaultDocumentCache(capacity));
            return services;
        }

        public static IServiceCollection AddOperationCache(
            this IServiceCollection services,
            int capacity = 100)
        {
            services.RemoveAll<IPreparedOperationCache>();
            services.AddSingleton<IPreparedOperationCache>(
                sp => new DefaultPreparedOperationCache(capacity));
            return services;
        }

        public static IServiceCollection AddMD5DocumentHashProvider(
            this IServiceCollection services,
            HashFormat format = HashFormat.Base64)
        {
            services.RemoveAll<IDocumentHashProvider>();
            services.AddSingleton<IDocumentHashProvider>(
                new MD5DocumentHashProvider(format));
            return services;
        }

        public static IServiceCollection AddSha1DocumentHashProvider(
            this IServiceCollection services,
            HashFormat format = HashFormat.Base64)
        {
            services.RemoveAll<IDocumentHashProvider>();
            services.AddSingleton<IDocumentHashProvider>(
                new Sha1DocumentHashProvider(format));
            return services;
        }

        public static IServiceCollection AddSha256DocumentHashProvider(
            this IServiceCollection services,
            HashFormat format = HashFormat.Base64)
        {
            services.RemoveAll<IDocumentHashProvider>();
            services.AddSingleton<IDocumentHashProvider>(
                new Sha256DocumentHashProvider(format));
            return services;
        }

        public static IServiceCollection AddBatchDispatcher<T>(this IServiceCollection services)
            where T : class, IBatchDispatcher
        {
            services.RemoveAll<IBatchDispatcher>();
            services.AddScoped<IBatchDispatcher, T>();
            return services;
        }

        public static IServiceCollection AddBatchScheduler<T>(this IServiceCollection services)
            where T : class, IBatchScheduler
        {
            services.RemoveAll<IBatchScheduler>();
            services.AddScoped<IBatchScheduler, T>();
            return services;
        }

        public static IServiceCollection AddDefaultBatchDispatcher(this IServiceCollection services)
        {
            services.RemoveAll<IBatchScheduler>();
            services.TryAddDefaultBatchDispatcher();
            return services;
        }
    }
}
