using HotChocolate.Execution.Configuration;
using HotChocolate.Subscriptions;
using HotChocolate.Subscriptions.Postgres;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// These helper methods allows to register the Postgres subscription provider with the GraphQL
/// configuration.
/// </summary>
public static class PostgresSubscriptionTransportExtensions
{
    /// <summary>
    /// Registers the Postgres subscription provider with the GraphQL configuration.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="configure">
    /// A delegate that configures the Postgres subscription provider options.
    /// </param>
    public static IRequestExecutorBuilder AddPostgresSubscriptions(
        this IRequestExecutorBuilder builder,
        Action<PostgresSubscriptionOptions> configure)
        => builder.AddPostgresSubscriptions((_, options) => configure(options));

    /// <summary>
    /// Registers the Postgres subscription provider with the GraphQL configuration.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="configure">
    /// A delegate that configures the Postgres subscription provider options.
    /// </param>
    public static IRequestExecutorBuilder AddPostgresSubscriptions(
        this IRequestExecutorBuilder builder,
        Action<IServiceProvider, PostgresSubscriptionOptions> configure)
    {
        builder.AddSubscriptionDiagnostics();

        var services = builder.Services;
        services.AddSingleton(sp =>
        {
            var options = new PostgresSubscriptionOptions();
            configure(sp, options);
            return options;
        });

        services.AddSingleton<IMessageSerializer, DefaultJsonMessageSerializer>();
        services.AddSingleton<PostgresChannel>(sp =>
        {
            var options = sp.GetRequiredService<PostgresSubscriptionOptions>();
            return new PostgresChannel(options);
        });

        services.AddSingleton<PostgresPubSub>();
        services.TryAddSingleton<ITopicEventSender>(
            sp => sp.GetRequiredService<PostgresPubSub>());
        services.TryAddSingleton<ITopicEventReceiver>(
            sp => sp.GetRequiredService<PostgresPubSub>());

        return builder;
    }
}
