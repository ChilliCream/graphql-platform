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
    /// Registers the composite <see cref="MochaResourceSource"/> that aggregates all contributors
    /// registered via <see cref="AddMochaResourceSource{TSource}"/>.
    /// </summary>
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
