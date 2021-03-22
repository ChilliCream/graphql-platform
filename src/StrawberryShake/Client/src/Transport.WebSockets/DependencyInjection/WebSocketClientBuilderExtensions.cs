using System;
using Microsoft.Extensions.Options;
using StrawberryShake;
using StrawberryShake.Transport.WebSockets;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Extension methods for configuring an <see cref="IWebSocketClientBuilder"/>
    /// </summary>
    public static class WebSocketClientBuilderExtensions
    {
        /// <summary>
        /// Adds the <see cref="ISocketClientFactory"/> and related services to the
        /// <see cref="IServiceCollection"/> and configures a <see cref="WebSocketClient"/>
        /// with the correct name
        /// </summary>
        /// <param name="clientBuilder">
        /// The <see cref="IClientBuilder{T}"/>
        /// </param>
        /// <param name="configureClient">
        /// A delegate that is used to configure an <see cref="WebSocketClient"/>.
        /// </param>
        public static IClientBuilder<T> ConfigureWebSocketClient<T>(
            this IClientBuilder<T> clientBuilder,
            Action<ISocketClient> configureClient)
            where T : IStoreAccessor
        {
            if (clientBuilder == null)
            {
                throw new ArgumentNullException(nameof(clientBuilder));
            }

            if (configureClient == null)
            {
                throw new ArgumentNullException(nameof(configureClient));
            }

            clientBuilder.Services.AddWebSocketClient(clientBuilder.ClientName, configureClient);
            return clientBuilder;
        }

        /// <summary>
        /// Adds the <see cref="ISocketClientFactory"/> and related services to the
        /// <see cref="IServiceCollection"/> and configures a <see cref="WebSocketClient"/>
        /// with the correct name
        /// </summary>
        /// <param name="clientBuilder">
        /// The <see cref="IClientBuilder{T}"/>
        /// </param>
        /// <param name="configureClient">
        /// A delegate that is used to configure an <see cref="WebSocketClient"/>.
        /// </param>
        public static IClientBuilder<T> ConfigureWebSocketClient<T>(
            this IClientBuilder<T> clientBuilder,
            Action<IServiceProvider, ISocketClient> configureClient)
            where T : IStoreAccessor
        {
            if (clientBuilder == null)
            {
                throw new ArgumentNullException(nameof(clientBuilder));
            }

            if (configureClient == null)
            {
                throw new ArgumentNullException(nameof(configureClient));
            }

            clientBuilder.Services.AddWebSocketClient(clientBuilder.ClientName, configureClient);
            return clientBuilder;
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

        /// <summary>
        /// Configures a <see cref="ISocketConnectionInterceptor"/> on this socket client
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IServiceCollection"/>.
        /// </param>
        /// <param name="interceptor">
        /// The interceptor that should be used
        /// </param>
        /// <returns>
        /// An <see cref="IWebSocketClientBuilder"/> that can be used to configure the client.
        /// </returns>
        public static IWebSocketClientBuilder ConfigureConnectionInterceptor(
            this IWebSocketClientBuilder builder,
            ISocketConnectionInterceptor interceptor)
        {
            return builder.ConfigureConnectionInterceptor(_ => interceptor);
        }

        /// <summary>
        /// Configures what type of <see cref="ISocketConnectionInterceptor"/> this socket client
        /// should use.
        ///
        /// Resolves the <typeparamref name="TInterceptor"/> from the dependency injection
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IServiceCollection"/>.
        /// </param>
        /// <typeparam name="TInterceptor">
        /// The type of the <see cref="ISocketConnectionInterceptor"/> that should be resolved from
        /// the dependency injection
        /// </typeparam>
        /// <returns>
        /// An <see cref="IWebSocketClientBuilder"/> that can be used to configure the client.
        /// </returns>
        public static IWebSocketClientBuilder ConfigureConnectionInterceptor<TInterceptor>(
            this IWebSocketClientBuilder builder)
            where TInterceptor : ISocketConnectionInterceptor
        {
            return builder
                .ConfigureConnectionInterceptor(sp => sp.GetRequiredService<TInterceptor>());
        }

        /// <summary>
        /// Configures a <see cref="ISocketConnectionInterceptor"/> on this socket client
        /// </summary>
        /// <param name="builder">
        /// The <see cref="IServiceCollection"/>.
        /// </param>
        /// <param name="factory">
        /// A delegate that creates a <see cref="ISocketConnectionInterceptor"/>
        /// </param>
        /// <returns>
        /// An <see cref="IWebSocketClientBuilder"/> that can be used to configure the client.
        /// </returns>
        public static IWebSocketClientBuilder ConfigureConnectionInterceptor(
            this IWebSocketClientBuilder builder,
            Func<IServiceProvider, ISocketConnectionInterceptor> factory)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            return builder
                .ConfigureWebSocketClient((sp, x) => x.ConnectionInterceptor = factory(sp));
        }
    }
}
