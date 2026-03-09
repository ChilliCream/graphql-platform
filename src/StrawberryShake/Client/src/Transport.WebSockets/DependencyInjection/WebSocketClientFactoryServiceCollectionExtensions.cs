using Microsoft.Extensions.DependencyInjection.Extensions;
using StrawberryShake.Transport.WebSockets;
using StrawberryShake.Transport.WebSockets.Protocols;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions methods to configure an <see cref="IServiceCollection"/> for
/// <see cref="ISocketClientFactory"/>.
/// </summary>
public static class WebSocketClientFactoryServiceCollectionExtensions
{
    /// <summary>
    /// Adds a websocket <see cref="ISocketProtocolFactory"/> to the <see cref="IServiceCollection"/>
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/>.
    /// </param>
    /// <returns>
    /// An <see cref="IWebSocketClientBuilder"/> that can be used to configure the client.
    /// </returns>
    public static IServiceCollection AddProtocol<TFactory>(this IServiceCollection services)
        where TFactory : class, ISocketProtocolFactory
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<ISocketProtocolFactory, TFactory>();

        return services;
    }

    /// <summary>
    /// Adds the <see cref="ISocketClientFactory"/> and related services
    /// to the <see cref="IServiceCollection"/> and configures a named
    /// <see cref="WebSocketClient"/>.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/>.
    /// </param>
    /// <param name="name">
    /// The logical name of the <see cref="WebSocketClient"/> to configure.
    /// </param>
    /// <returns>
    /// An <see cref="IWebSocketClientBuilder"/> that can be used to configure the client.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <see cref="WebSocketClient"/> instances that apply the provided configuration can
    /// be retrieved using <see cref="ISocketClientFactory.CreateClient(string)"/>
    /// and providing the matching name.
    /// </para>
    /// <para>
    /// Use <see cref="Options.Options.DefaultName"/> as the name to configure the default client.
    /// </para>
    /// </remarks>
    public static IWebSocketClientBuilder AddWebSocketClient(
        this IServiceCollection services,
        string name)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(name);

        AddWebSocketClient(services);

        return new DefaultWebSocketClientBuilder(services, name);
    }

    /// <summary>
    /// Adds the <see cref="ISocketClientFactory"/> and related services
    /// to the <see cref="IServiceCollection"/> and configures a named
    /// <see cref="WebSocketClient"/>.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/>.
    /// </param>
    /// <param name="name">
    /// The logical name of the <see cref="WebSocketClient"/> to configure.
    /// </param>
    /// <param name="configureClient">
    /// A delegate that is used to configure an <see cref="WebSocketClient"/>.
    /// </param>
    /// <returns>
    /// An <see cref="ISocketClientFactory"/> that can be used to configure the client.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <see cref="WebSocketClient"/> instances that apply the provided
    /// configuration can be retrieved using
    /// <see cref="ISocketClientFactory.CreateClient(string)"/> and providing
    /// the matching name.
    /// </para>
    /// <para>
    /// Use <see cref="Options.Options.DefaultName"/> as the name to configure the default client.
    /// </para>
    /// </remarks>
    public static IWebSocketClientBuilder AddWebSocketClient(
        this IServiceCollection services,
        string name,
        Action<IWebSocketClient> configureClient)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(configureClient);

        AddWebSocketClient(services);

        var builder = new DefaultWebSocketClientBuilder(services, name);
        builder.ConfigureWebSocketClient(configureClient);
        return builder;
    }

    /// <summary>
    /// Adds the <see cref="ISocketClientFactory"/> and related services
    /// to the <see cref="IServiceCollection"/> and configures a named
    /// <see cref="WebSocketClient"/>.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/>.
    /// </param>
    /// <param name="name">
    /// The logical name of the <see cref="WebSocketClient"/> to configure.
    /// </param>
    /// <param name="configureClient">
    /// A delegate that is used to configure an <see cref="WebSocketClient"/>.
    /// </param>
    /// <returns>
    /// An <see cref="ISocketClientFactory"/> that can be used to configure the client.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <see cref="WebSocketClient"/> instances that apply the provided
    /// configuration can be retrieved using
    /// <see cref="ISocketClientFactory.CreateClient(string)"/> and providing
    /// the matching name.
    /// </para>
    /// <para>
    /// Use <see cref="Options.Options.DefaultName"/> as the name to configure the default client.
    /// </para>
    /// </remarks>
    public static IWebSocketClientBuilder AddWebSocketClient(
        this IServiceCollection services,
        string name,
        Action<IServiceProvider, IWebSocketClient> configureClient)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(configureClient);

        AddWebSocketClient(services);

        var builder = new DefaultWebSocketClientBuilder(services, name);
        builder.ConfigureWebSocketClient(configureClient);
        return builder;
    }

    private static IServiceCollection AddWebSocketClient(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions();

        services.TryAddSingleton<IEnumerable<ISocketProtocolFactory>>(
            new ISocketProtocolFactory[]
            {
                new GraphQLWebSocketProtocolFactory()
            });
        services.TryAddSingleton<DefaultSocketClientFactory>();
        services.TryAddSingleton<ISocketClientFactory>(sp =>
            sp.GetRequiredService<DefaultSocketClientFactory>());
        services.AddWebSocketClientPool();

        return services;
    }
}
