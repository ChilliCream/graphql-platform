using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Subscriptions;
using HotChocolate.Fusion.Subscriptions.AmazonSqs;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides service registration helpers for Amazon SQS event stream brokers.
/// </summary>
public static class AmazonSqsEventStreamBrokerServiceCollectionExtensions
{
    /// <summary>
    /// Registers Amazon SQS as the default Fusion event stream broker.
    /// </summary>
    /// <param name="builder">
    /// The Fusion gateway builder.
    /// </param>
    /// <param name="configure">
    /// An optional callback used to configure the Amazon SQS connection.
    /// </param>
    /// <returns>
    /// The same <see cref="IFusionGatewayBuilder"/> instance so additional calls can be chained.
    /// </returns>
    public static IFusionGatewayBuilder AddAmazonSqsEventStreamBroker(
        this IFusionGatewayBuilder builder,
        Action<AmazonSqsEventStreamOptions>? configure = null)
        => builder.AddAmazonSqsEventStreamBroker(name: null, configure);

    /// <summary>
    /// Registers Amazon SQS as a named Fusion event stream broker.
    /// </summary>
    /// <param name="builder">
    /// The Fusion gateway builder.
    /// </param>
    /// <param name="name">
    /// The broker name used by the execution schema, or <c>null</c> to register the default broker.
    /// </param>
    /// <param name="configure">
    /// An optional callback used to configure the Amazon SQS connection.
    /// </param>
    /// <returns>
    /// The same <see cref="IFusionGatewayBuilder"/> instance so additional calls can be chained.
    /// </returns>
    public static IFusionGatewayBuilder AddAmazonSqsEventStreamBroker(
        this IFusionGatewayBuilder builder,
        string? name,
        Action<AmazonSqsEventStreamOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddAmazonSqsEventStreamBroker(name, configure);

        return builder;
    }

    /// <summary>
    /// Registers Amazon SQS as the default Fusion event stream broker.
    /// </summary>
    /// <param name="services">
    /// The service collection to configure.
    /// </param>
    /// <param name="configure">
    /// An optional callback used to configure the Amazon SQS connection.
    /// </param>
    /// <returns>
    /// The same <see cref="IServiceCollection"/> instance so additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddAmazonSqsEventStreamBroker(
        this IServiceCollection services,
        Action<AmazonSqsEventStreamOptions>? configure = null)
        => services.AddAmazonSqsEventStreamBroker(name: null, configure);

    /// <summary>
    /// Registers Amazon SQS as a named Fusion event stream broker.
    /// </summary>
    /// <param name="services">
    /// The service collection to configure.
    /// </param>
    /// <param name="name">
    /// The broker name used by the execution schema, or <c>null</c> to register the default broker.
    /// </param>
    /// <param name="configure">
    /// An optional callback used to configure the Amazon SQS connection.
    /// </param>
    /// <returns>
    /// The same <see cref="IServiceCollection"/> instance so additional calls can be chained.
    /// </returns>
    public static IServiceCollection AddAmazonSqsEventStreamBroker(
        this IServiceCollection services,
        string? name,
        Action<AmazonSqsEventStreamOptions>? configure = null)
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
            services.AddOptions<AmazonSqsEventStreamOptions>(key);
        }

        services.TryAddKeyedSingleton<IEventStreamBrokerProvider>(
            key,
            static (sp, k) => new AmazonSqsEventStreamBrokerProvider(
                (string)k!,
                sp.GetRequiredService<IOptionsMonitor<AmazonSqsEventStreamOptions>>()));

        return services;
    }
}
