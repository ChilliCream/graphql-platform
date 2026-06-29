using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Subscriptions;
using HotChocolate.Fusion.Subscriptions.Kafka;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides service registration helpers for Kafka event stream brokers.
/// </summary>
public static class KafkaEventStreamBrokerServiceCollectionExtensions
{
    /// <summary>
    /// Registers Kafka as the default Fusion event stream broker.
    /// </summary>
    /// <param name="builder">
    /// The Fusion gateway builder.
    /// </param>
    /// <param name="configure">
    /// An optional callback used to configure the Kafka consumer settings.
    /// </param>
    /// <returns>
    /// The same <see cref="IFusionGatewayBuilder"/> instance so additional calls can be chained.
    /// </returns>
    public static IFusionGatewayBuilder AddKafkaEventStreamBroker(
        this IFusionGatewayBuilder builder,
        Action<KafkaEventStreamOptions>? configure = null)
        => builder.AddKafkaEventStreamBroker(name: null, configure);

    /// <summary>
    /// Registers Kafka as a named Fusion event stream broker.
    /// </summary>
    /// <param name="builder">
    /// The Fusion gateway builder.
    /// </param>
    /// <param name="name">
    /// The broker name used by the execution schema, or <c>null</c> to register the default broker.
    /// </param>
    /// <param name="configure">
    /// An optional callback used to configure the Kafka consumer settings.
    /// </param>
    /// <returns>
    /// The same <see cref="IFusionGatewayBuilder"/> instance so additional calls can be chained.
    /// </returns>
    public static IFusionGatewayBuilder AddKafkaEventStreamBroker(
        this IFusionGatewayBuilder builder,
        string? name,
        Action<KafkaEventStreamOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddKafkaEventStreamBroker(name, configure);

        return builder;
    }

    /// <summary>
    /// Registers Kafka as the default Fusion event stream broker.
    /// </summary>
    /// <param name="services">
    /// The service collection to configure.
    /// </param>
    /// <param name="configure">
    /// An optional callback used to configure the Kafka consumer settings.
    /// </param>
    /// <returns>
    /// The same <see cref="IServiceCollection"/> instance so additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddKafkaEventStreamBroker(
        this IServiceCollection services,
        Action<KafkaEventStreamOptions>? configure = null)
        => services.AddKafkaEventStreamBroker(name: null, configure);

    /// <summary>
    /// Registers Kafka as a named Fusion event stream broker.
    /// </summary>
    /// <param name="services">
    /// The service collection to configure.
    /// </param>
    /// <param name="name">
    /// The broker name used by the execution schema, or <c>null</c> to register the default broker.
    /// </param>
    /// <param name="configure">
    /// An optional callback used to configure the Kafka consumer settings.
    /// </param>
    /// <returns>
    /// The same <see cref="IServiceCollection"/> instance so additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddKafkaEventStreamBroker(
        this IServiceCollection services,
        string? name,
        Action<KafkaEventStreamOptions>? configure = null)
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
            services.AddOptions<KafkaEventStreamOptions>(key);
        }

        services.TryAddKeyedSingleton<IEventStreamBrokerProvider>(
            key,
            static (sp, k) =>
            {
                // The event-stream abstraction has no request/reply surface.
                return new KafkaEventStreamBrokerProvider(
                    (string)k!,
                    sp.GetRequiredService<IOptionsMonitor<KafkaEventStreamOptions>>());
            });

        return services;
    }
}
