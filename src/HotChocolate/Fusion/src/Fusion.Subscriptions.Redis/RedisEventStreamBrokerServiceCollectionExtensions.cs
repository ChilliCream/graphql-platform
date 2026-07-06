using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Subscriptions;
using HotChocolate.Fusion.Subscriptions.Redis;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides service registration helpers for Redis event stream brokers.
/// </summary>
public static class RedisEventStreamBrokerServiceCollectionExtensions
{
    /// <summary>
    /// Registers Redis as the default Fusion event stream broker.
    /// </summary>
    /// <param name="builder">
    /// The Fusion gateway builder.
    /// </param>
    /// <param name="configure">
    /// An optional callback used to configure the Redis connection.
    /// </param>
    /// <returns>
    /// The same <see cref="IFusionGatewayBuilder"/> instance so additional calls can be chained.
    /// </returns>
    public static IFusionGatewayBuilder AddRedisEventStreamBroker(
        this IFusionGatewayBuilder builder,
        Action<RedisEventStreamOptions>? configure = null)
        => builder.AddRedisEventStreamBroker(name: null, configure);

    /// <summary>
    /// Registers Redis as a named Fusion event stream broker.
    /// </summary>
    /// <param name="builder">
    /// The Fusion gateway builder.
    /// </param>
    /// <param name="name">
    /// The broker name used by the execution schema, or <c>null</c> to register the default broker.
    /// </param>
    /// <param name="configure">
    /// An optional callback used to configure the Redis connection.
    /// </param>
    /// <returns>
    /// The same <see cref="IFusionGatewayBuilder"/> instance so additional calls can be chained.
    /// </returns>
    public static IFusionGatewayBuilder AddRedisEventStreamBroker(
        this IFusionGatewayBuilder builder,
        string? name,
        Action<RedisEventStreamOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddRedisEventStreamBroker(name, configure);

        return builder;
    }

    /// <summary>
    /// Registers Redis as the default Fusion event stream broker.
    /// </summary>
    /// <param name="services">
    /// The service collection to configure.
    /// </param>
    /// <param name="configure">
    /// An optional callback used to configure the Redis connection.
    /// </param>
    /// <returns>
    /// The same <see cref="IServiceCollection"/> instance so additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddRedisEventStreamBroker(
        this IServiceCollection services,
        Action<RedisEventStreamOptions>? configure = null)
        => services.AddRedisEventStreamBroker(name: null, configure);

    /// <summary>
    /// Registers Redis as a named Fusion event stream broker.
    /// </summary>
    /// <param name="services">
    /// The service collection to configure.
    /// </param>
    /// <param name="name">
    /// The broker name used by the execution schema, or <c>null</c> to register the default broker.
    /// </param>
    /// <param name="configure">
    /// An optional callback used to configure the Redis connection.
    /// </param>
    /// <returns>
    /// The same <see cref="IServiceCollection"/> instance so additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddRedisEventStreamBroker(
        this IServiceCollection services,
        string? name,
        Action<RedisEventStreamOptions>? configure = null)
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
            services.AddOptions<RedisEventStreamOptions>(key);
        }

        services.TryAddKeyedSingleton<IEventStreamBrokerProvider>(
            key,
            static (sp, k) => new RedisEventStreamBrokerProvider(
                (string)k!,
                sp.GetRequiredService<IOptionsMonitor<RedisEventStreamOptions>>()));

        return services;
    }
}
