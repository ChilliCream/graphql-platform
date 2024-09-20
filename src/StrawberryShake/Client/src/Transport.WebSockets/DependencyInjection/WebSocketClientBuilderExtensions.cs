using Microsoft.Extensions.Options;
using StrawberryShake;
using StrawberryShake.Transport.WebSockets;

namespace Microsoft.Extensions.DependencyInjection;

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
    /// <param name="configureClientBuilder">
    /// A delegate that is used to additionally configure the <see cref="IWebSocketClient"/>
    /// with a <see cref="IWebSocketClientBuilder"/>
    /// </param>
    public static IClientBuilder<T> ConfigureWebSocketClient<T>(
        this IClientBuilder<T> clientBuilder,
        Action<IWebSocketClient> configureClient,
        Action<IWebSocketClientBuilder>? configureClientBuilder = null)
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

        var builder = clientBuilder.Services
            .AddWebSocketClient(clientBuilder.ClientName, configureClient);

        configureClientBuilder?.Invoke(builder);

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
    /// <param name="configureClientBuilder">
    /// A delegate that is used to additionally configure the <see cref="IWebSocketClient"/>
    /// with a <see cref="IWebSocketClientBuilder"/>
    /// </param>
    public static IClientBuilder<T> ConfigureWebSocketClient<T>(
        this IClientBuilder<T> clientBuilder,
        Action<IServiceProvider, IWebSocketClient> configureClient,
        Action<IWebSocketClientBuilder>? configureClientBuilder = null)
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

        var builder = clientBuilder.Services
            .AddWebSocketClient(clientBuilder.ClientName, configureClient);

        configureClientBuilder?.Invoke(builder);

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
        Action<IWebSocketClient> configureClient)
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
            options => options.SocketClientActions.Add(
                socketClient =>
                {
                    if (socketClient is IWebSocketClient webSocketClient)
                    {
                        configureClient(webSocketClient);
                    }
                }));

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
        Action<IServiceProvider, IWebSocketClient> configureClient)
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
                    socketClient =>
                    {
                        if (socketClient is IWebSocketClient webSocketClient)
                        {
                            configureClient(sp, webSocketClient);
                        }
                    })));

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
