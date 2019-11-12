using System;
using System.Net.WebSockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace StrawberryShake.Transport.WebSockets
{
    /// <summary>
    /// Extension methods for configuring an <see cref="IClientWebSocketBuilder"/>
    /// </summary>
    public static class ClientWebSocketBuilderExtensions
    {
        /// <summary>
        /// Adds a delegate that will be used to configure a named <see cref="ClientWebSocket"/>.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IServiceCollection"/>.
        /// </param>
        /// <param name="configureClient">
        /// A delegate that is used to configure an <see cref="ClientWebSocket"/>.
        /// </param>
        /// <returns>
        /// An <see cref="IClientWebSocketBuilder"/> that can be used to configure the client.
        /// </returns>
        public static IClientWebSocketBuilder ConfigureClientWebSocket(
            this IClientWebSocketBuilder builder,
            Action<ClientWebSocket> configureClient)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configureClient == null)
            {
                throw new ArgumentNullException(nameof(configureClient));
            }

            builder.Services.Configure<ClientWebSocketFactoryOptions>(
                builder.Name,
                options => options.ClientWebSocketActions.Add(configureClient));

            return builder;
        }

        /// <summary>
        /// Adds a delegate that will be used to configure a named <see cref="ClientWebSocket"/>.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IServiceCollection"/>.
        /// </param>
        /// <param name="configureClient">
        /// A delegate that is used to configure an <see cref="ClientWebSocket"/>.
        /// </param>
        /// <returns>
        /// An <see cref="IClientWebSocketBuilder"/> that can be used to configure the client.
        /// </returns>
        /// <remarks>
        /// The <see cref="IServiceProvider"/> provided to <paramref name="configureClient"/>
        /// will be the application's root service provider instance.
        /// </remarks>
        public static IClientWebSocketBuilder ConfigureHttpClient(
            this IClientWebSocketBuilder builder,
            Action<IServiceProvider, ClientWebSocket> configureClient)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configureClient == null)
            {
                throw new ArgumentNullException(nameof(configureClient));
            }

            builder.Services.AddTransient<IConfigureOptions<ClientWebSocketFactoryOptions>>(sp =>
            {
                return new ConfigureNamedOptions<ClientWebSocketFactoryOptions>(
                    builder.Name,
                    options => options.ClientWebSocketActions.Add(
                        client => configureClient(sp, client)));
            });

            return builder;
        }
    }
}
