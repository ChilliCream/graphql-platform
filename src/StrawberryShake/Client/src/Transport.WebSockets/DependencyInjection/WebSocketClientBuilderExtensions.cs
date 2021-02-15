using System;
using Microsoft.Extensions.Options;
using StrawberryShake.Transport.WebSockets;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for configuring an <see cref="IWebSocketClientBuilder"/>
    /// </summary>
    public static class WebSocketClientBuilderExtensions
    {
        /// <summary>
        /// Adds a delegate that will be used to configure a named <see cref="WebSocketClient"/>.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IServiceCollection"/>.
        /// </param>
        /// <param name="configureClient">
        /// A delegate that is used to configure an <see cref="WebSocketClient"/>.
        /// </param>
        /// <returns>
        /// An <see cref="IWebSocketClientBuilder"/> that can be used to configure the client.
        /// </returns>
        public static IWebSocketClientBuilder ConfigureWebSocketClient(
            this IWebSocketClientBuilder builder,
            Action<ISocketClient> configureClient)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configureClient == null)
            {
                throw new ArgumentNullException(nameof(configureClient));
            }

            builder.Services.Configure<SocketClientFactoryOptions>(
                builder.Name,
                options => options.SocketClientActions.Add(configureClient));

            return builder;
        }

        /// <summary>
        /// Adds a delegate that will be used to configure a named <see cref="WebSocketClient"/>.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IServiceCollection"/>.
        /// </param>
        /// <param name="configureClient">
        /// A delegate that is used to configure an <see cref="WebSocketClient"/>.
        /// </param>
        /// <returns>
        /// An <see cref="IWebSocketClientBuilder"/> that can be used to configure the client.
        /// </returns>
        /// <remarks>
        /// The <see cref="IServiceProvider"/> provided to <paramref name="configureClient"/>
        /// will be the application's root service provider instance.
        /// </remarks>
        public static IWebSocketClientBuilder ConfigureWebSocketClient(
            this IWebSocketClientBuilder builder,
            Action<IServiceProvider, ISocketClient> configureClient)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (configureClient == null)
            {
                throw new ArgumentNullException(nameof(configureClient));
            }

            builder.Services.AddTransient<IConfigureOptions<SocketClientFactoryOptions>>(sp =>
                new ConfigureNamedOptions<SocketClientFactoryOptions>(
                    builder.Name,
                    options => options.SocketClientActions.Add(
                        client => configureClient(sp, client))));

            return builder;
        }
    }
}
