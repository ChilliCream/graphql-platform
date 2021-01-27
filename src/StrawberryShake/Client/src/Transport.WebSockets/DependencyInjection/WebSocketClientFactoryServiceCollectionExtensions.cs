using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace StrawberryShake.Transport.WebSockets
{
    /// <summary>
    /// Extensions methods to configure an <see cref="IServiceCollection"/> for <see cref="ISocketClientFactory"/>.
    /// </summary>
    public static class WebSocketClientFactoryServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the <see cref="ISocketClientFactory"/> and related services to the <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddWebSocketClient(
            this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddOptions();

            services.AddSingleton<DefaultSocketClientFactory>();
            services.TryAddSingleton<ISocketClientFactory>(sp =>
                sp.GetRequiredService<DefaultSocketClientFactory>());
            services.AddWebSocketClientPool();

            return services;
        }

        /// <summary>
        /// Adds the <see cref="ISocketClientFactory"/> and related services
        /// to the <see cref="IServiceCollection"/> and configures a named
        /// <see cref="SocketClient"/>.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/>.
        /// </param>
        /// <param name="name">
        /// The logical name of the <see cref="SocketClient"/> to configure.
        /// </param>
        /// <returns>
        /// An <see cref="IWebSocketClientBuilder"/> that can be used to configure the client.
        /// </returns>
        /// <remarks>
        /// <para>
        /// <see cref="SocketClient"/> instances that apply the provided configuration can
        /// be retrieved using <see cref="ISocketClientFactory.CreateClient(string)"/>
        /// and providing the matching name.
        /// </para>
        /// <para>
        /// Use <see cref="Options.DefaultName"/> as the name to configure the
        /// default client.
        /// </para>
        /// </remarks>
        public static IWebSocketClientBuilder AddWebSocketClient(
            this IServiceCollection services,
            string name)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            AddWebSocketClient(services);

            return new DefaultWebSocketClientBuilder(services, name);
        }

        /// <summary>
        /// Adds the <see cref="ISocketClientFactory"/> and related services
        /// to the <see cref="IServiceCollection"/> and configures a named
        /// <see cref="SocketClient"/>.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/>.
        /// </param>
        /// <param name="name">
        /// The logical name of the <see cref="SocketClient"/> to configure.
        /// </param>
        /// <param name="configureClient">
        /// A delegate that is used to configure an <see cref="SocketClient"/>.
        /// </param>
        /// <returns>
        /// An <see cref="ISocketClientFactory"/> that can be used to configure the client.
        /// </returns>
        /// <remarks>
        /// <para>
        /// <see cref="SocketClient"/> instances that apply the provided
        /// configuration can be retrieved using
        /// <see cref="ISocketClientFactory.CreateClient(string)"/> and providing
        /// the matching name.
        /// </para>
        /// <para>
        /// Use <see cref="Options.DefaultName"/> as the name to configure
        /// the default client.
        /// </para>
        /// </remarks>
        public static IWebSocketClientBuilder AddWebSocketClient(
            this IServiceCollection services,
            string name,
            Action<ISocketClient> configureClient)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (configureClient == null)
            {
                throw new ArgumentNullException(nameof(configureClient));
            }

            AddWebSocketClient(services);

            var builder = new DefaultWebSocketClientBuilder(services, name);
            builder.ConfigureWebSocketClient(configureClient);
            return builder;
        }

        /// <summary>
        /// Adds the <see cref="ISocketClientFactory"/> and related services
        /// to the <see cref="IServiceCollection"/> and configures a named
        /// <see cref="SocketClient"/>.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/>.
        /// </param>
        /// <param name="name">
        /// The logical name of the <see cref="SocketClient"/> to configure.
        /// </param>
        /// <param name="configureClient">
        /// A delegate that is used to configure an <see cref="SocketClient"/>.
        /// </param>
        /// <returns>
        /// An <see cref="ISocketClientFactory"/> that can be used to configure the client.
        /// </returns>
        /// <remarks>
        /// <para>
        /// <see cref="SocketClient"/> instances that apply the provided
        /// configuration can be retrieved using
        /// <see cref="ISocketClientFactory.CreateClient(string)"/> and providing
        /// the matching name.
        /// </para>
        /// <para>
        /// Use <see cref="Options.DefaultName"/> as the name to configure
        /// the default client.
        /// </para>
        /// </remarks>
        public static IWebSocketClientBuilder AddWebSocketClient(
            this IServiceCollection services,
            string name,
            Action<IServiceProvider, ISocketClient> configureClient)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (configureClient == null)
            {
                throw new ArgumentNullException(nameof(configureClient));
            }

            AddWebSocketClient(services);

            var builder = new DefaultWebSocketClientBuilder(services, name);
            builder.ConfigureWebSocketClient(configureClient);
            return builder;
        }
    }
}
