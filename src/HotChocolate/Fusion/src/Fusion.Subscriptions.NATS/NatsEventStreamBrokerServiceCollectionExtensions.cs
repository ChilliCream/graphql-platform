using HotChocolate.Fusion.Subscriptions;
using HotChocolate.Fusion.Subscriptions.NATS;
using HotChocolate.Fusion.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

public static class NatsEventStreamBrokerServiceCollectionExtensions
{
    public static IFusionGatewayBuilder AddNatsEventStreamBroker(
        this IFusionGatewayBuilder builder,
        Action<NatsEventStreamOptions>? configure = null)
        => builder.AddNatsEventStreamBroker(name: null, configure);

    public static IFusionGatewayBuilder AddNatsEventStreamBroker(
        this IFusionGatewayBuilder builder,
        string? name,
        Action<NatsEventStreamOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddNatsEventStreamBroker(name, configure);

        return builder;
    }

    public static IServiceCollection AddNatsEventStreamBroker(
        this IServiceCollection services,
        Action<NatsEventStreamOptions>? configure = null)
        => services.AddNatsEventStreamBroker(name: null, configure);

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
