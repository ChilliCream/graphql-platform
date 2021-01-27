using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace StrawberryShake.Transport.WebSockets
{
    /// <summary>
    /// Extension methods for configuring an <see cref="IWebSocketClientBuilder"/>
    /// </summary>
    public static class WebSocketClientBuilderExtensions
    {
        /// <summary>
        /// Adds a websocket <see cref="ISocketProtocol"/> to the <see cref="WebSocketClient"/>
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IServiceCollection"/>.
        /// </param>
        /// <param name="protocol">
        /// A instance of a protocol to use
        /// </param>
        /// <returns>
        /// An <see cref="IWebSocketClientBuilder"/> that can be used to configure the client.
        /// </returns>
        public static IWebSocketClientBuilder AddProtocol(
            this IWebSocketClientBuilder builder,
            ISocketProtocol protocol)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (protocol == null)
            {
                throw new ArgumentNullException(nameof(protocol));
            }

            builder.Services.Configure<SocketClientFactoryOptions>(
                builder.Name,
                options => options.Protocols.Add(protocol));

            return builder;
        }

        /// <summary>
        /// Adds a delegate that will be used to configure a named <see cref="SocketClient"/>.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IServiceCollection"/>.
        /// </param>
        /// <param name="configureClient">
        /// A delegate that is used to configure an <see cref="SocketClient"/>.
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
                options => options.WebSocketClientActions.Add(configureClient));

            return builder;
        }

        /// <summary>
        /// Adds a delegate that will be used to configure a named <see cref="SocketClient"/>.
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IServiceCollection"/>.
        /// </param>
        /// <param name="configureClient">
        /// A delegate that is used to configure an <see cref="SocketClient"/>.
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
                    options => options.WebSocketClientActions.Add(
                        client => configureClient(sp, client))));

            return builder;
        }
    }
}
