using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Subscriptions;
using HotChocolate.Fusion.Subscriptions.AzureEventHubs;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides service registration helpers for Azure Event Hubs event stream brokers.
/// </summary>
public static class AzureEventHubsEventStreamBrokerServiceCollectionExtensions
{
    /// <summary>
    /// Registers Azure Event Hubs as the default Fusion event stream broker.
    /// </summary>
    /// <param name="builder">
    /// The Fusion gateway builder.
    /// </param>
    /// <param name="configure">
    /// An optional callback used to configure the Azure Event Hubs connection.
    /// </param>
    /// <returns>
    /// The same <see cref="IFusionGatewayBuilder"/> instance so additional calls can be chained.
    /// </returns>
    public static IFusionGatewayBuilder AddAzureEventHubsEventStreamBroker(
        this IFusionGatewayBuilder builder,
        Action<AzureEventHubsEventStreamOptions>? configure = null)
        => builder.AddAzureEventHubsEventStreamBroker(name: null, configure);

    /// <summary>
    /// Registers Azure Event Hubs as a named Fusion event stream broker.
    /// </summary>
    /// <param name="builder">
    /// The Fusion gateway builder.
    /// </param>
    /// <param name="name">
    /// The broker name used by the execution schema, or <c>null</c> to register the default broker.
    /// </param>
    /// <param name="configure">
    /// An optional callback used to configure the Azure Event Hubs connection.
    /// </param>
    /// <returns>
    /// The same <see cref="IFusionGatewayBuilder"/> instance so additional calls can be chained.
    /// </returns>
    public static IFusionGatewayBuilder AddAzureEventHubsEventStreamBroker(
        this IFusionGatewayBuilder builder,
        string? name,
        Action<AzureEventHubsEventStreamOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddAzureEventHubsEventStreamBroker(name, configure);

        return builder;
    }

    /// <summary>
    /// Registers Azure Event Hubs as the default Fusion event stream broker.
    /// </summary>
    /// <param name="services">
    /// The service collection to configure.
    /// </param>
    /// <param name="configure">
    /// An optional callback used to configure the Azure Event Hubs connection.
    /// </param>
    /// <returns>
    /// The same <see cref="IServiceCollection"/> instance so additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddAzureEventHubsEventStreamBroker(
        this IServiceCollection services,
        Action<AzureEventHubsEventStreamOptions>? configure = null)
        => services.AddAzureEventHubsEventStreamBroker(name: null, configure);

    /// <summary>
    /// Registers Azure Event Hubs as a named Fusion event stream broker.
    /// </summary>
    /// <param name="services">
    /// The service collection to configure.
    /// </param>
    /// <param name="name">
    /// The broker name used by the execution schema, or <c>null</c> to register the default broker.
    /// </param>
    /// <param name="configure">
    /// An optional callback used to configure the Azure Event Hubs connection.
    /// </param>
    /// <returns>
    /// The same <see cref="IServiceCollection"/> instance so additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddAzureEventHubsEventStreamBroker(
        this IServiceCollection services,
        string? name,
        Action<AzureEventHubsEventStreamOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var key = name ?? DefaultEventStreamBrokerFactory.DefaultBrokerKey;

        services.TryAddSingleton<IEventStreamBrokerFactory, DefaultEventStreamBrokerFactory>();

        if (configure is not null)
        {
            services.Configure(key, configure);
        }
        else
        {
            services.AddOptions<AzureEventHubsEventStreamOptions>(key);
        }

        services.TryAddKeyedSingleton<IEventStreamBrokerProvider>(
            key,
            static (sp, k) => new AzureEventHubsEventStreamBrokerProvider(
                (string)k!,
                sp.GetRequiredService<IOptionsMonitor<AzureEventHubsEventStreamOptions>>()));

        return services;
    }
}
