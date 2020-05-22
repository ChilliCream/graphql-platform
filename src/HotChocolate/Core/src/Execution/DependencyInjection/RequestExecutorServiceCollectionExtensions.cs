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

            // executor services
            services.TryAddOperationContext();
            services.TryAddRequestExecutorFactory();

            return services;
        }

        /// <summary>
        /// Adds the <see cref="IHttpClientFactory"/> and related services to the <see cref="IServiceCollection"/> and configures
        /// a named <see cref="HttpClient"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="name">The logical name of the <see cref="HttpClient"/> to configure.</param>
        /// <returns>An <see cref="IHttpClientBuilder"/> that can be used to configure the client.</returns>
        /// <remarks>
        /// <para>
        /// <see cref="HttpClient"/> instances that apply the provided configuration can be retrieved using 
        /// <see cref="IHttpClientFactory.CreateClient(string)"/> and providing the matching name.
        /// </para>
        /// <para>
        /// Use <see cref="Options.Options.DefaultName"/> as the name to configure the default client.
        /// </para>
        /// </remarks>
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