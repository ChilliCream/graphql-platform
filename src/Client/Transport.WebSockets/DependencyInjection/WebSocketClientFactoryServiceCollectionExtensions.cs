using System;
using System.Net.WebSockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace StrawberryShake.Transport.WebSockets
{
    /// <summary>
    /// Extensions methods to configure an <see cref="IServiceCollection"/> for <see cref="IWebSocketClientFactory"/>.
    /// </summary>
    public static class WebSocketClientFactoryServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the <see cref="IWebSocketClientFactory"/> and related services to the <see cref="IServiceCollection"/>.
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

            services.AddSingleton<DefaultWebSocketClientFactory>();
            services.TryAddSingleton<IWebSocketClientFactory>(sp =>
                sp.GetRequiredService<DefaultWebSocketClientFactory>());

            return services;
        }

        /// <summary>
        /// Adds the <see cref="IWebSocketClientFactory"/> and related services
        /// to the <see cref="IServiceCollection"/> and configures a named
        /// <see cref="ClientWebSocket"/>.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/>.
        /// </param>
        /// <param name="name">
        /// The logical name of the <see cref="ClientWebSocket"/> to configure.
        /// </param>
        /// <returns>
        /// An <see cref="IClientWebSocketBuilder"/> that can be used to configure the client.
        /// </returns>
        /// <remarks>
        /// <para>
        /// <see cref="ClientWebSocket"/> instances that apply the provided configuration can
        /// be retrieved using <see cref="IWebSocketClientFactory.CreateClient(string)"/>
        /// and providing the matching name.
        /// </para>
        /// <para>
        /// Use <see cref="Options.Options.DefaultName"/> as the name to configure the
        /// default client.
        /// </para>
        /// </remarks>
        public static IClientWebSocketBuilder AddWebSocketClient(
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

            return new DefaultClientWebSocketBuilder(services, name);
        }

        /// <summary>
        /// Adds the <see cref="IWebSocketClientFactory"/> and related services
        /// to the <see cref="IServiceCollection"/> and configures a named
        /// <see cref="ClientWebSocket"/>.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/>.
        /// </param>
        /// <param name="name">
        /// The logical name of the <see cref="ClientWebSocket"/> to configure.
        /// </param>
        /// <param name="configureClient">
        /// A delegate that is used to configure an <see cref="ClientWebSocket"/>.
        /// </param>
        /// <returns>
        /// An <see cref="IWebSocketClientFactory"/> that can be used to configure the client.
        /// </returns>
        /// <remarks>
        /// <para>
        /// <see cref="ClientWebSocket"/> instances that apply the provided
        /// configuration can be retrieved using
        /// <see cref="IWebSocketClientFactory.CreateClient(string)"/> and providing
        /// the matching name.
        /// </para>
        /// <para>
        /// Use <see cref="Options.Options.DefaultName"/> as the name to configure
        /// the default client.
        /// </para>
        /// </remarks>
        public static IClientWebSocketBuilder AddWebSocketClient(
            this IServiceCollection services,
            string name,
            Action<ClientWebSocket> configureClient)
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

            var builder = new DefaultClientWebSocketBuilder(services, name);
            builder.ConfigureClientWebSocket(configureClient);
            return builder;
        }

        /// <summary>
        /// Adds the <see cref="IWebSocketClientFactory"/> and related services
        /// to the <see cref="IServiceCollection"/> and configures a named
        /// <see cref="ClientWebSocket"/>.
        /// </summary>
        /// <param name="services">
        /// The <see cref="IServiceCollection"/>.
        /// </param>
        /// <param name="name">
        /// The logical name of the <see cref="ClientWebSocket"/> to configure.
        /// </param>
        /// <param name="configureClient">
        /// A delegate that is used to configure an <see cref="ClientWebSocket"/>.
        /// </param>
        /// <returns>
        /// An <see cref="IWebSocketClientFactory"/> that can be used to configure the client.
        /// </returns>
        /// <remarks>
        /// <para>
        /// <see cref="ClientWebSocket"/> instances that apply the provided
        /// configuration can be retrieved using
        /// <see cref="IWebSocketClientFactory.CreateClient(string)"/> and providing
        /// the matching name.
        /// </para>
        /// <para>
        /// Use <see cref="Options.Options.DefaultName"/> as the name to configure
        /// the default client.
        /// </para>
        /// </remarks>
        public static IClientWebSocketBuilder AddWebSocketClient(
            this IServiceCollection services,
            string name,
            Action<IServiceProvider, ClientWebSocket> configureClient)
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

            var builder = new DefaultClientWebSocketBuilder(services, name);
            builder.ConfigureHttpClient(configureClient);
            return builder;
        }
    }
}
