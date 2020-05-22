using System;
using HotChocolate.Execution.Configuration;

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
            services.TryAddVariableCoercion();
            services.TryAddResultPool();
            services.TryAddResolverTaskPool();
            services.TryAddTypeConversion();

            // executor services
            services.TryAddRequestExecutorResolver();
            services.TryAddOperationContext();

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

            return new DefaultRequestExecutorBuilder(services, name);
        }
    }
}