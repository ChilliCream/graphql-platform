using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Policies.Rego;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides extension methods to register Rego policy support on a Fusion gateway.
/// </summary>
public static class RegoFusionGatewayBuilderExtensions
{
    /// <summary>
    /// Registers the Rego policy provider so that policies packaged as Rego take effect on the
    /// gateway.
    /// </summary>
    /// <param name="builder">The Fusion gateway builder.</param>
    /// <returns>The Fusion gateway builder.</returns>
    public static IFusionGatewayBuilder AddRegoPolicies(this IFusionGatewayBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.ConfigureSchemaServices(
            static (_, services) =>
            {
                // Exactly one policy provider is active per gateway: the first registration wins,
                // so calling this alongside another policy provider registration is a no-op for
                // whichever one runs second.
                services.TryAddSingleton<IPolicyProvider>(
                    static sp => new RegoPolicyProvider(
                        sp.GetRequiredService<IFusionExecutionDiagnosticEvents>(),
                        CreateDataAggregatorOrNull(sp)));
            });
    }

    /// <summary>
    /// Registers a Rego data provider that is constructed and owned by the schema services
    /// container. The provider is identified by its type's full name, which must be unique among
    /// every data provider registered on this gateway.
    /// </summary>
    /// <typeparam name="T">The data provider implementation.</typeparam>
    /// <param name="builder">The Fusion gateway builder.</param>
    /// <returns>The Fusion gateway builder.</returns>
    public static IFusionGatewayBuilder AddRegoDataProvider<T>(this IFusionGatewayBuilder builder)
        where T : class, IRegoDataProvider
    {
        ArgumentNullException.ThrowIfNull(builder);

        var name = typeof(T).FullName ?? typeof(T).Name;

        return builder.ConfigureSchemaServices(
            (_, services) =>
            {
                services.TryAddSingleton<T>();

                // The type is a container-managed singleton, so the container already disposes it;
                // the aggregator must not dispose it a second time.
                services.AddSingleton(
                    new RegoDataProviderRegistration(
                        name,
                        static sp => sp.GetRequiredService<T>(),
                        ownsInstance: false));
            });
    }

    /// <summary>
    /// Registers a caller-owned Rego data provider instance under the given name.
    /// </summary>
    /// <param name="builder">The Fusion gateway builder.</param>
    /// <param name="name">
    /// The provider's name, used in diagnostics and required to be unique among every data
    /// provider registered on this gateway.
    /// </param>
    /// <param name="instance">
    /// The provider instance. The caller retains ownership; it is never disposed by the gateway.
    /// </param>
    /// <returns>The Fusion gateway builder.</returns>
    public static IFusionGatewayBuilder AddRegoDataProvider(
        this IFusionGatewayBuilder builder,
        string name,
        IRegoDataProvider instance)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(instance);

        return builder.ConfigureSchemaServices(
            (_, services) => services.AddSingleton(
                new RegoDataProviderRegistration(name, _ => instance, ownsInstance: false)));
    }

    /// <summary>
    /// Registers a Rego data provider produced by a factory under the given name. The produced
    /// instance is owned by the gateway and disposed together with it.
    /// </summary>
    /// <param name="builder">The Fusion gateway builder.</param>
    /// <param name="name">
    /// The provider's name, used in diagnostics and required to be unique among every data
    /// provider registered on this gateway.
    /// </param>
    /// <param name="factory">The factory that produces the provider instance.</param>
    /// <returns>The Fusion gateway builder.</returns>
    public static IFusionGatewayBuilder AddRegoDataProvider(
        this IFusionGatewayBuilder builder,
        string name,
        Func<IServiceProvider, IRegoDataProvider> factory)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(factory);

        return builder.ConfigureSchemaServices(
            (_, services) => services.AddSingleton(
                new RegoDataProviderRegistration(name, factory, ownsInstance: true)));
    }

    private static RegoDataAggregator? CreateDataAggregatorOrNull(IServiceProvider services)
    {
        var registrations = new List<RegoDataProviderRegistration>(
            services.GetServices<RegoDataProviderRegistration>());

        if (registrations.Count == 0)
        {
            return null;
        }

        var diagnosticEvents = services.GetRequiredService<IFusionExecutionDiagnosticEvents>();
        var logger = services.GetService<ILogger<RegoDataAggregator>>()
            ?? (ILogger)NullLogger<RegoDataAggregator>.Instance;

        var aggregator = new RegoDataAggregator(registrations, services, diagnosticEvents, logger);
        aggregator.Start();
        return aggregator;
    }
}
