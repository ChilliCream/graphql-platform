using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Mocha.Resources;

/// <summary>
/// Provides extension methods on <see cref="IServiceCollection"/> for registering Mocha resource
/// sources and the composite consumer view.
/// </summary>
public static class MochaResourcesServiceCollectionExtensions
{
    /// <summary>
    /// Registers the composite <see cref="MochaResourceSource"/> consumed by diagnostic surfaces
    /// (cloud-bugfix's Nitro endpoint, in-process tools, …).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Contributors (message bus core, transports, sagas, …) register themselves via
    /// <see cref="AddMochaResourceSource{TSource}"/>. This method aggregates all such
    /// contributors into a singleton <see cref="CompositeMochaResourceSource"/> and exposes it as
    /// the resolved <see cref="MochaResourceSource"/> service. Consumers call
    /// <c>services.GetService&lt;MochaResourceSource&gt;()</c> and receive the composite view.
    /// </para>
    /// <para>
    /// Idempotent — repeat calls keep the first composite registration.
    /// </para>
    /// </remarks>
    /// <param name="services">The service collection to register services into.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for fluent chaining.</returns>
    public static IServiceCollection AddMochaResources(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<CompositeMochaResourceSource>(static sp =>
            new CompositeMochaResourceSource(sp.GetServices<IMochaResourceSourceContributor>().Select(static c => c.Source)));

        services.TryAddSingleton<MochaResourceSource>(static sp =>
            sp.GetRequiredService<CompositeMochaResourceSource>());

        services.TryAddSingleton<IMochaResourceDefinitionCatalog>(static sp =>
            new MochaResourceDefinitionCatalog(sp.GetServices<MochaResourceDefinition>()));

        return services;
    }

    /// <summary>
    /// Registers <typeparamref name="TSource"/> as a singleton <see cref="MochaResourceSource"/>
    /// contributor that will be aggregated by the composite installed by
    /// <see cref="AddMochaResources"/>.
    /// </summary>
    /// <remarks>
    /// The same instance is also exposed both as <typeparamref name="TSource"/> and as a
    /// contributor-marker behind the scenes, so consumers that want the concrete subclass can
    /// still resolve it directly.
    /// </remarks>
    /// <typeparam name="TSource">The concrete source subclass.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for fluent chaining.</returns>
    public static IServiceCollection AddMochaResourceSource<TSource>(this IServiceCollection services)
        where TSource : MochaResourceSource
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<TSource>();
        services.AddSingleton<IMochaResourceSourceContributor>(
            static sp => new MochaResourceSourceContributor(sp.GetRequiredService<TSource>()));
        return services;
    }

    /// <summary>
    /// Registers an existing <see cref="MochaResourceSource"/> instance as a contributor.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="source">The source instance to register.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for fluent chaining.</returns>
    public static IServiceCollection AddMochaResourceSource(this IServiceCollection services, MochaResourceSource source)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(source);

        services.AddSingleton<IMochaResourceSourceContributor>(new MochaResourceSourceContributor(source));
        return services;
    }
}
