using Microsoft.Extensions.DependencyInjection.Extensions;
using StrawberryShake.Transport.InMemory;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions methods to configure an <see cref="IServiceCollection"/> for
/// <see cref="IInMemoryClientFactory"/>.
/// </summary>
public static class InMemoryClientFactoryServiceCollectionExtensions
{
    /// <summary>
    /// Adds the <see cref="IInMemoryClientFactory"/> and related services
    /// to the <see cref="IServiceCollection"/> and configures a named
    /// <see cref="InMemoryClient"/>.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/>.
    /// </param>
    /// <param name="name">
    /// The logical name of the <see cref="InMemoryClient"/> to configure.
    /// </param>
    /// <returns>
    /// An <see cref="IInMemoryClientBuilder"/> that can be used to configure the client.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <see cref="InMemoryClient"/> instances that apply the provided configuration can
    /// be retrieved using <see cref="IInMemoryClientFactory.CreateAsync"/>
    /// and providing the matching name.
    /// </para>
    /// <para>
    /// Use <see cref="Options.Options.DefaultName"/> as the name to configure the default client.
    /// </para>
    /// </remarks>
    public static IInMemoryClientBuilder AddInMemoryClient(
        this IServiceCollection services,
        string name)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(name);

        AddInMemoryClient(services);

        return new DefaultInMemoryClientBuilder(services, name);
    }

    /// <summary>
    /// Adds the <see cref="IInMemoryClientFactory"/> and related services
    /// to the <see cref="IServiceCollection"/> and configures a named
    /// <see cref="InMemoryClient"/>.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/>.
    /// </param>
    /// <param name="name">
    /// The logical name of the <see cref="InMemoryClient"/> to configure.
    /// </param>
    /// <param name="configureClient">
    /// A delegate that is used to configure an <see cref="InMemoryClient"/>.
    /// </param>
    /// <returns>
    /// An <see cref="IInMemoryClientFactory"/> that can be used to configure the client.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <see cref="InMemoryClient"/> instances that apply the provided
    /// configuration can be retrieved using
    /// <see cref="IInMemoryClientFactory.CreateAsync"/> and providing
    /// the matching name.
    /// </para>
    /// <para>
    /// Use <see cref="Options.Options.DefaultName"/> as the name to configure the default client.
    /// </para>
    /// </remarks>
    public static IInMemoryClientBuilder AddInMemoryClient(
        this IServiceCollection services,
        string name,
        Action<IInMemoryClient> configureClient)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(configureClient);

        AddInMemoryClient(services);

        var builder = new DefaultInMemoryClientBuilder(services, name);
        builder.ConfigureInMemoryClient(configureClient);
        return builder;
    }

    /// <summary>
    /// Adds the <see cref="IInMemoryClientFactory"/> and related services
    /// to the <see cref="IServiceCollection"/> and configures a named
    /// <see cref="InMemoryClient"/>.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/>.
    /// </param>
    /// <param name="name">
    /// The logical name of the <see cref="InMemoryClient"/> to configure.
    /// </param>
    /// <param name="configureClient">
    /// A delegate that is used to configure an <see cref="InMemoryClient"/>.
    /// </param>
    /// <returns>
    /// An <see cref="IInMemoryClientFactory"/> that can be used to configure the client.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <see cref="InMemoryClient"/> instances that apply the provided
    /// configuration can be retrieved using
    /// <see cref="IInMemoryClientFactory.CreateAsync"/> and providing
    /// the matching name.
    /// </para>
    /// <para>
    /// Use <see cref="Options.Options.DefaultName"/> as the name to configure the default client.
    /// </para>
    /// </remarks>
    public static IInMemoryClientBuilder AddInMemoryClient(
        this IServiceCollection services,
        string name,
        Action<IServiceProvider, IInMemoryClient> configureClient)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(configureClient);

        AddInMemoryClient(services);

        var builder = new DefaultInMemoryClientBuilder(services, name);
        builder.ConfigureInMemoryClient(configureClient);
        return builder;
    }

    /// <summary>
    /// Adds the <see cref="IInMemoryClientFactory"/> and related services
    /// to the <see cref="IServiceCollection"/> and configures a named
    /// <see cref="InMemoryClient"/>.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/>.
    /// </param>
    /// <param name="name">
    /// The logical name of the <see cref="InMemoryClient"/> to configure.
    /// </param>
    /// <param name="configureClientAsync">
    /// A delegate that is used to configure an <see cref="InMemoryClient"/>.
    /// </param>
    /// <returns>
    /// An <see cref="IInMemoryClientFactory"/> that can be used to configure the client.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <see cref="InMemoryClient"/> instances that apply the provided
    /// configuration can be retrieved using
    /// <see cref="IInMemoryClientFactory.CreateAsync"/> and providing
    /// the matching name.
    /// </para>
    /// <para>
    /// Use <see cref="Options.Options.DefaultName"/> as the name to configure the default client.
    /// </para>
    /// </remarks>
    public static IInMemoryClientBuilder AddInMemoryClientAsync(
        this IServiceCollection services,
        string name,
        Func<IInMemoryClient, CancellationToken, ValueTask> configureClientAsync)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(configureClientAsync);

        AddInMemoryClient(services);

        var builder = new DefaultInMemoryClientBuilder(services, name);
        builder.ConfigureInMemoryClientAsync(configureClientAsync);
        return builder;
    }

    /// <summary>
    /// Adds the <see cref="IInMemoryClientFactory"/> and related services
    /// to the <see cref="IServiceCollection"/> and configures a named
    /// <see cref="InMemoryClient"/>.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/>.
    /// </param>
    /// <param name="name">
    /// The logical name of the <see cref="InMemoryClient"/> to configure.
    /// </param>
    /// <param name="configureClientAsync">
    /// A delegate that is used to configure an <see cref="InMemoryClient"/>.
    /// </param>
    /// <returns>
    /// An <see cref="IInMemoryClientFactory"/> that can be used to configure the client.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <see cref="InMemoryClient"/> instances that apply the provided
    /// configuration can be retrieved using
    /// <see cref="IInMemoryClientFactory.CreateAsync"/> and providing
    /// the matching name.
    /// </para>
    /// <para>
    /// Use <see cref="Options.Options.DefaultName"/> as the name to configure the default client.
    /// </para>
    /// </remarks>
    public static IInMemoryClientBuilder AddInMemoryClientAsync(
        this IServiceCollection services,
        string name,
        Func<IServiceProvider, IInMemoryClient, CancellationToken, ValueTask>
            configureClientAsync)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(configureClientAsync);

        AddInMemoryClient(services);

        var builder = new DefaultInMemoryClientBuilder(services, name);
        builder.ConfigureInMemoryClientAsync(configureClientAsync);
        return builder;
    }

    private static IServiceCollection AddInMemoryClient(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions();

        services.TryAddSingleton<DefaultInMemoryClientFactory>();
        services.TryAddSingleton<IInMemoryClientFactory>(sp =>
            sp.GetRequiredService<DefaultInMemoryClientFactory>());

        return services;
    }
}
