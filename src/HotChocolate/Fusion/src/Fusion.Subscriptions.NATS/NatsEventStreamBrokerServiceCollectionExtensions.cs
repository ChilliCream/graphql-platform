using HotChocolate.Fusion.Subscriptions;
using HotChocolate.Fusion.Subscriptions.NATS;
using HotChocolate.Fusion.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides service registration helpers for NATS event stream brokers.
/// </summary>
public static class NatsEventStreamBrokerServiceCollectionExtensions
{
    /// <summary>
    /// Registers NATS as the default Fusion event stream broker.
    /// </summary>
    /// <param name="builder">
    /// The Fusion gateway builder.
    /// </param>
    /// <param name="configure">
    /// An optional callback used to configure the NATS connection and JetStream settings.
    /// </param>
    /// <returns>
    /// The same <see cref="IFusionGatewayBuilder"/> instance so additional calls can be chained.
    /// </returns>
    public static IFusionGatewayBuilder AddNatsEventStreamBroker(
        this IFusionGatewayBuilder builder,
        Action<NatsEventStreamOptions>? configure = null)
        => builder.AddNatsEventStreamBroker(name: null, configure);

    /// <summary>
    /// Registers NATS as a named Fusion event stream broker.
    /// </summary>
    /// <param name="builder">
    /// The Fusion gateway builder.
    /// </param>
    /// <param name="name">
    /// The broker name used by the execution schema, or <c>null</c> to register the default broker.
    /// </param>
    /// <param name="configure">
    /// An optional callback used to configure the NATS connection and JetStream settings.
    /// </param>
    /// <returns>
    /// The same <see cref="IFusionGatewayBuilder"/> instance so additional calls can be chained.
    /// </returns>
    public static IFusionGatewayBuilder AddNatsEventStreamBroker(
        this IFusionGatewayBuilder builder,
        string? name,
        Action<NatsEventStreamOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddNatsEventStreamBroker(name, configure);

        return builder;
    }

    /// <summary>
    /// Registers NATS as the default Fusion event stream broker.
    /// </summary>
    /// <param name="services">
    /// The service collection to configure.
    /// </param>
    /// <param name="configure">
    /// An optional callback used to configure the NATS connection and JetStream settings.
    /// </param>
    /// <returns>
    /// The same <see cref="IServiceCollection"/> instance so additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddNatsEventStreamBroker(
        this IServiceCollection services,
        Action<NatsEventStreamOptions>? configure = null)
        => services.AddNatsEventStreamBroker(name: null, configure);

    /// <summary>
    /// Registers NATS as a named Fusion event stream broker.
    /// </summary>
    /// <param name="services">
    /// The service collection to configure.
    /// </param>
    /// <param name="name">
    /// The broker name used by the execution schema, or <c>null</c> to register the default broker.
    /// </param>
    /// <param name="configure">
    /// An optional callback used to configure the NATS connection and JetStream settings.
    /// </param>
    /// <returns>
    /// The same <see cref="IServiceCollection"/> instance so additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddNatsEventStreamBroker(
        this IServiceCollection services,
        string? name,
        Action<NatsEventStreamOptions>? configure = null)
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
            services.AddOptions<NatsEventStreamOptions>(key);
        }

        services.TryAddKeyedSingleton<IEventStreamBrokerProvider>(
            key,
            static (sp, k) => new NatsEventStreamBrokerProvider(
                (string)k!,
                sp.GetRequiredService<IOptionsMonitor<NatsEventStreamOptions>>()));

        return services;
    }
}
