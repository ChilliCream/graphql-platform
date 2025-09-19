using Microsoft.Extensions.Options;
using StrawberryShake;
using StrawberryShake.Transport.InMemory;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring an <see cref="IInMemoryClientBuilder"/>
/// </summary>
public static class InMemoryClientBuilderExtensions
{
    /// <summary>
    /// Adds the <see cref="IInMemoryClientFactory"/> and related services to the
    /// <see cref="IServiceCollection"/> and configures a <see cref="InMemoryClient"/>
    /// with the correct name and the default schema of the <see cref="IServiceCollection"/>
    /// </summary>
    /// <param name="clientBuilder">
    /// The <see cref="IClientBuilder{T}"/>
    /// </param>
    public static IClientBuilder<T> ConfigureInMemoryClient<T>(
        this IClientBuilder<T> clientBuilder)
        where T : IStoreAccessor
    {
        ArgumentNullException.ThrowIfNull(clientBuilder);

        clientBuilder.Services.AddInMemoryClient(clientBuilder.ClientName);
        return clientBuilder;
    }

    /// <summary>
    /// Adds the <see cref="IInMemoryClientFactory"/> and related services to the
    /// <see cref="IServiceCollection"/> and configures a <see cref="InMemoryClient"/>
    /// with the correct name
    /// </summary>
    /// <param name="clientBuilder">
    /// The <see cref="IClientBuilder{T}"/>
    /// </param>
    /// <param name="configureClient">
    /// A delegate that is used to configure an <see cref="InMemoryClient"/>.
    /// </param>
    public static IClientBuilder<T> ConfigureInMemoryClient<T>(
        this IClientBuilder<T> clientBuilder,
        Action<IInMemoryClient> configureClient)
        where T : IStoreAccessor
    {
        ArgumentNullException.ThrowIfNull(clientBuilder);
        ArgumentNullException.ThrowIfNull(configureClient);

        clientBuilder.Services.AddInMemoryClient(clientBuilder.ClientName, configureClient);
        return clientBuilder;
    }

    /// <summary>
    /// Adds the <see cref="IInMemoryClientFactory"/> and related services to the
    /// <see cref="IServiceCollection"/> and configures a <see cref="InMemoryClient"/>
    /// with the correct name
    /// </summary>
    /// <param name="clientBuilder">
    /// The <see cref="IClientBuilder{T}"/>
    /// </param>
    /// <param name="configureClient">
    /// A delegate that is used to configure an <see cref="InMemoryClient"/>.
    /// </param>
    public static IClientBuilder<T> ConfigureInMemoryClient<T>(
        this IClientBuilder<T> clientBuilder,
        Action<IServiceProvider, IInMemoryClient> configureClient)
        where T : IStoreAccessor
    {
        ArgumentNullException.ThrowIfNull(clientBuilder);
        ArgumentNullException.ThrowIfNull(configureClient);

        clientBuilder.Services.AddInMemoryClient(clientBuilder.ClientName, configureClient);
        return clientBuilder;
    }

    /// <summary>
    /// Adds the <see cref="IInMemoryClientFactory"/> and related services to the
    /// <see cref="IServiceCollection"/> and configures a <see cref="InMemoryClient"/>
    /// with the correct name
    /// </summary>
    /// <param name="clientBuilder">
    /// The <see cref="IClientBuilder{T}"/>
    /// </param>
    /// <param name="configureClient">
    /// A delegate that is used to configure an <see cref="InMemoryClient"/>.
    /// </param>
    public static IClientBuilder<T> ConfigureInMemoryClientAsync<T>(
        this IClientBuilder<T> clientBuilder,
        Func<IInMemoryClient, CancellationToken, ValueTask> configureClient)
        where T : IStoreAccessor
    {
        ArgumentNullException.ThrowIfNull(clientBuilder);
        ArgumentNullException.ThrowIfNull(configureClient);

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
    /// The <see cref="IClientBuilder{T}"/>
    /// </param>
    /// <param name="configureClient">
    /// A delegate that is used to configure an <see cref="InMemoryClient"/>.
    /// </param>
    public static IClientBuilder<T> ConfigureInMemoryClientAsync<T>(
        this IClientBuilder<T> clientBuilder,
        Func<IServiceProvider, IInMemoryClient, CancellationToken, ValueTask> configureClient)
        where T : IStoreAccessor
    {
        ArgumentNullException.ThrowIfNull(clientBuilder);
        ArgumentNullException.ThrowIfNull(configureClient);

        clientBuilder.Services
            .AddInMemoryClientAsync(clientBuilder.ClientName, configureClient);

        return clientBuilder;
    }

    /// <summary>
    /// Adds a delegate that will be used to configure a named <see cref="InMemoryClient"/>.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IServiceCollection"/>.
    /// </param>
    /// <param name="configureClient">
    /// A delegate that is used to configure an <see cref="InMemoryClient"/>.
    /// </param>
    /// <returns>
    /// An <see cref="IInMemoryClientBuilder"/> that can be used to configure the client.
    /// </returns>
    public static IInMemoryClientBuilder ConfigureInMemoryClient(
        this IInMemoryClientBuilder builder,
        Action<IInMemoryClient> configureClient)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureClient);

        builder.Services.Configure<InMemoryClientFactoryOptions>(
            builder.Name,
            options => options.InMemoryClientActions.Add(
                (client, _) =>
                {
                    configureClient(client);
                    return default;
                }));

        return builder;
    }

    /// <summary>
    /// Adds a delegate that will be used to configure a named <see cref="InMemoryClient"/>.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IServiceCollection"/>.
    /// </param>
    /// <param name="configureClientAsync">
    /// A delegate that is used to configure an <see cref="InMemoryClient"/>.
    /// </param>
    /// <returns>
    /// An <see cref="IInMemoryClientBuilder"/> that can be used to configure the client.
    /// </returns>
    public static IInMemoryClientBuilder ConfigureInMemoryClientAsync(
        this IInMemoryClientBuilder builder,
        Func<IInMemoryClient, CancellationToken, ValueTask> configureClientAsync)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureClientAsync);

        builder.Services.Configure<InMemoryClientFactoryOptions>(
            builder.Name,
            options =>
                options.InMemoryClientActions.Add(
                    (client, token) => configureClientAsync(client, token)));

        return builder;
    }

    /// <summary>
    /// Adds a delegate that will be used to configure a named <see cref="InMemoryClient"/>.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IServiceCollection"/>.
    /// </param>
    /// <param name="configureClient">
    /// A delegate that is used to configure an <see cref="InMemoryClient"/>.
    /// </param>
    /// <returns>
    /// An <see cref="IInMemoryClientBuilder"/> that can be used to configure the client.
    /// </returns>
    /// <remarks>
    /// The <see cref="IServiceProvider"/> provided to <paramref name="configureClient"/>
    /// will be the application's root service provider instance.
    /// </remarks>
    public static IInMemoryClientBuilder ConfigureInMemoryClient(
        this IInMemoryClientBuilder builder,
        Action<IServiceProvider, IInMemoryClient> configureClient)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureClient);

        builder.Services.AddTransient<IConfigureOptions<InMemoryClientFactoryOptions>>(sp =>
            new ConfigureNamedOptions<InMemoryClientFactoryOptions>(
                builder.Name,
                options => options.InMemoryClientActions.Add(
                    (client, _) =>
                    {
                        configureClient(sp, client);
                        return default;
                    })));

        return builder;
    }

    /// <summary>
    /// Adds a delegate that will be used to configure a named <see cref="InMemoryClient"/>.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IServiceCollection"/>.
    /// </param>
    /// <param name="configureClient">
    /// A delegate that is used to configure an <see cref="InMemoryClient"/>.
    /// </param>
    /// <returns>
    /// An <see cref="IInMemoryClientBuilder"/> that can be used to configure the client.
    /// </returns>
    /// <remarks>
    /// The <see cref="IServiceProvider"/> provided to <paramref name="configureClient"/>
    /// will be the application's root service provider instance.
    /// </remarks>
    public static IInMemoryClientBuilder ConfigureInMemoryClientAsync(
        this IInMemoryClientBuilder builder,
        Func<IServiceProvider, IInMemoryClient, CancellationToken, ValueTask> configureClient)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configureClient);

        builder.Services.AddTransient<IConfigureOptions<InMemoryClientFactoryOptions>>(sp =>
            new ConfigureNamedOptions<InMemoryClientFactoryOptions>(
                builder.Name,
                options => options.InMemoryClientActions.Add(
                    (client, ct) => configureClient(sp, client, ct))));

        return builder;
    }

    /// <summary>
    /// Configures a <see cref="IInMemoryRequestInterceptor"/> on this socket client
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IServiceCollection"/>.
    /// </param>
    /// <param name="interceptor">
    /// The interceptor that should be used
    /// </param>
    /// <returns>
    /// An <see cref="IInMemoryClientBuilder"/> that can be used to configure the client.
    /// </returns>
    public static IInMemoryClientBuilder ConfigureRequestInterceptor(
        this IInMemoryClientBuilder builder,
        IInMemoryRequestInterceptor interceptor)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(interceptor);

        return builder.ConfigureRequestInterceptor(_ => interceptor);
    }

    /// <summary>
    /// Configures what type of <see cref="IInMemoryRequestInterceptor"/> this socket client
    /// should use.
    ///
    /// Resolves the <typeparamref name="TInterceptor"/> from the dependency injection
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IServiceCollection"/>.
    /// </param>
    /// <typeparam name="TInterceptor">
    /// The type of the <see cref="IInMemoryRequestInterceptor"/> that should be resolved from
    /// the dependency injection
    /// </typeparam>
    /// <returns>
    /// An <see cref="IInMemoryClientBuilder"/> that can be used to configure the client.
    /// </returns>
    public static IInMemoryClientBuilder ConfigureRequestInterceptor<TInterceptor>(
        this IInMemoryClientBuilder builder)
        where TInterceptor : IInMemoryRequestInterceptor
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder
            .ConfigureRequestInterceptor(sp => sp.GetRequiredService<TInterceptor>());
    }

    /// <summary>
    /// Configures a <see cref="IInMemoryRequestInterceptor"/> on this socket client
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IServiceCollection"/>.
    /// </param>
    /// <param name="factory">
    /// A delegate that creates a <see cref="IInMemoryRequestInterceptor"/>
    /// </param>
    /// <returns>
    /// An <see cref="IInMemoryClientBuilder"/> that can be used to configure the client.
    /// </returns>
    public static IInMemoryClientBuilder ConfigureRequestInterceptor(
        this IInMemoryClientBuilder builder,
        Func<IServiceProvider, IInMemoryRequestInterceptor> factory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(factory);

        return builder
            .ConfigureInMemoryClient((sp, x) => x.RequestInterceptors.Add(factory(sp)));
    }
}
