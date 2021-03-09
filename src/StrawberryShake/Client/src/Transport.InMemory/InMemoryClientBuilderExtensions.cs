using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake.Transport.InMemory;

namespace StrawberryShake
{
    /// <summary>
    /// Common extensions of <see cref="IClientBuilder"/> for <see cref="InMemoryConnection"/>
    /// </summary>
    public static class InMemoryClientBuilderExtensions
    {
        /// <summary>
        /// Adds the <see cref="IInMemoryClientFactory"/> and related services to the
        /// <see cref="IServiceCollection"/> and configures a <see cref="InMemoryClient"/>
        /// with the correct name and the default schema of the <see cref="IServiceCollection"/>
        /// </summary>
        /// <param name="clientBuilder">
        /// The <see cref="IClientBuilder"/>
        /// </param>
        public static IClientBuilder ConfigureInMemoryClient(this IClientBuilder clientBuilder)
        {
            if (clientBuilder == null)
            {
                throw new ArgumentNullException(nameof(clientBuilder));
            }

            clientBuilder.Services.AddInMemoryClient(clientBuilder.ClientName);
            return clientBuilder;
        }

        /// <summary>
        /// Adds the <see cref="IInMemoryClientFactory"/> and related services to the
        /// <see cref="IServiceCollection"/> and configures a <see cref="InMemoryClient"/>
        /// with the correct name
        /// </summary>
        /// <param name="clientBuilder">
        /// The <see cref="IClientBuilder"/>
        /// </param>
        /// <param name="configureClient">
        /// A delegate that is used to configure an <see cref="InMemoryClient"/>.
        /// </param>
        public static IClientBuilder ConfigureInMemoryClient(
            this IClientBuilder clientBuilder,
            Action<IInMemoryClient> configureClient)
        {
            if (clientBuilder == null)
            {
                throw new ArgumentNullException(nameof(clientBuilder));
            }

            if (configureClient == null)
            {
                throw new ArgumentNullException(nameof(configureClient));
            }

            clientBuilder.Services.AddInMemoryClient(clientBuilder.ClientName, configureClient);
            return clientBuilder;
        }

        /// <summary>
        /// Adds the <see cref="IInMemoryClientFactory"/> and related services to the
        /// <see cref="IServiceCollection"/> and configures a <see cref="InMemoryClient"/>
        /// with the correct name
        /// </summary>
        /// <param name="clientBuilder">
        /// The <see cref="IClientBuilder"/>
        /// </param>
        /// <param name="configureClient">
        /// A delegate that is used to configure an <see cref="InMemoryClient"/>.
        /// </param>
        public static IClientBuilder ConfigureInMemoryClient(
            this IClientBuilder clientBuilder,
            Action<IServiceProvider, IInMemoryClient> configureClient)
        {
            if (clientBuilder == null)
            {
                throw new ArgumentNullException(nameof(clientBuilder));
            }

            if (configureClient == null)
            {
                throw new ArgumentNullException(nameof(configureClient));
            }

            clientBuilder.Services.AddInMemoryClient(clientBuilder.ClientName, configureClient);
            return clientBuilder;
        }

        /// <summary>
        /// Adds the <see cref="IInMemoryClientFactory"/> and related services to the
        /// <see cref="IServiceCollection"/> and configures a <see cref="InMemoryClient"/>
        /// with the correct name
        /// </summary>
        /// <param name="clientBuilder">
        /// The <see cref="IClientBuilder"/>
        /// </param>
        /// <param name="configureClient">
        /// A delegate that is used to configure an <see cref="InMemoryClient"/>.
        /// </param>
        public static IClientBuilder ConfigureInMemoryClientAsync(
            this IClientBuilder clientBuilder,
            Func<IInMemoryClient, CancellationToken, ValueTask> configureClient)
        {
            if (clientBuilder == null)
            {
                throw new ArgumentNullException(nameof(clientBuilder));
            }

            if (configureClient == null)
            {
                throw new ArgumentNullException(nameof(configureClient));
            }

            clientBuilder.Services
                .AddInMemoryClientAsync(clientBuilder.ClientName, configureClient);
            return clientBuilder;
        }

        /// <summary>
        /// Adds the <see cref="IInMemoryClientFactory"/> and related services to the
        /// <see cref="IServiceCollection"/> and configures a <see cref="InMemoryClient"/>
        /// with the correct name
        /// </summary>
        /// <param name="clientBuilder">
        /// The <see cref="IClientBuilder"/>
        /// </param>
        /// <param name="configureClient">
        /// A delegate that is used to configure an <see cref="InMemoryClient"/>.
        /// </param>
        public static IClientBuilder ConfigureInMemoryClientAsync(
            this IClientBuilder clientBuilder,
            Func<IServiceProvider, IInMemoryClient, CancellationToken, ValueTask> configureClient)
        {
            if (clientBuilder == null)
            {
                throw new ArgumentNullException(nameof(clientBuilder));
            }

            if (configureClient == null)
            {
                throw new ArgumentNullException(nameof(configureClient));
            }

            clientBuilder.Services
                .AddInMemoryClientAsync(clientBuilder.ClientName, configureClient);

            return clientBuilder;
        }
    }
}
