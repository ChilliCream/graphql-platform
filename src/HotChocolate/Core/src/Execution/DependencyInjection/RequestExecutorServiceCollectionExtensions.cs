using System;
using HotChocolate.Execution.Caching;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddOptions();

            // core services
            services.TryAddTypeConversion();
            services.TryAddNoOpDiagnostics();
            services.TryAddDefaultCaches();

            // pools
            services.TryAddResultPool();
            services.TryAddResolverTaskPool();
            services.TryAddOperationContextPool();

            // executor services
            services.TryAddVariableCoercion();
            services.TryAddOperationExecutors();
            services.TryAddRequestExecutorResolver();

            return services;
        }

        /// <summary>
        /// Adds the <see cref="IRequestExecutorResolver"/> and related services to the 
        /// <see cref="IServiceCollection"/> and configures a named <see cref="IRequestExecutor"/>.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/>.
        /// </param>
        /// <param name="name">
        /// The logical name of the <see cref="IRequestExecutor"/> to configure.
        /// </param>
        /// <returns>
        /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure the executor.
        /// </returns>
        public static IRequestExecutorBuilder AddGraphQL(
            this IServiceCollection services,
            string? name = null)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            AddGraphQLCore(services);
            services.AddValidation(name);

            return new DefaultRequestExecutorBuilder(services, name);
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
    }
}