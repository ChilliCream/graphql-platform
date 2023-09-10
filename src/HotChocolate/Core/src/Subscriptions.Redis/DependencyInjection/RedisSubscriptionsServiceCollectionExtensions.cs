using HotChocolate.Execution.Configuration;
using HotChocolate.Subscriptions;
using HotChocolate.Subscriptions.Diagnostics;
using HotChocolate.Subscriptions.Redis;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// These helper methods allows to register the Redis subscription
/// provider with the GraphQL configuration.
/// </summary>
public static class RedisSubscriptionsServiceCollectionExtensions
{
    /// <summary>
    /// Adds support for using Redis as a subscription provider.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="connection">
    /// A factory to resolve or create the Redis connection object-
    /// </param>
    /// <param name="options">
    /// The subscription provider options.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c>.
    /// <paramref name="connection"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder AddRedisSubscriptions(
        this IRequestExecutorBuilder builder,
        Func<IServiceProvider, IConnectionMultiplexer> connection,
        SubscriptionOptions? options = null)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (connection is null)
        {
            throw new ArgumentNullException(nameof(connection));
        }

        builder.AddSubscriptionDiagnostics();
        AddRedisSubscriptions(builder.Services, connection, options);
        return builder;
    }

    /// <summary>
    /// Adds support for using Redis as a subscription provider.
    /// Ensure that the <see cref="IConnectionMultiplexer"/> is registered as a service.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <param name="options">
    /// The subscription provider options.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder for configuration chaining.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder AddRedisSubscriptions(
        this IRequestExecutorBuilder builder,
        SubscriptionOptions? options = null)
        => builder.AddRedisSubscriptions(
            sp => sp.GetRequiredService<IConnectionMultiplexer>(),
            options);

    private static void AddRedisSubscriptions(
        this IServiceCollection services,
        Func<IServiceProvider, IConnectionMultiplexer> connection,
        SubscriptionOptions? options = null)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (connection is null)
        {
            throw new ArgumentNullException(nameof(connection));
        }

        services.TryAddSingleton<SubscriptionOptions>(_ => options ?? new SubscriptionOptions());
        services.TryAddSingleton<IMessageSerializer, DefaultJsonMessageSerializer>();
        services.TryAddSingleton(sp => new RedisPubSub(
            connection(sp),
            sp.GetRequiredService<IMessageSerializer>(),
            sp.GetRequiredService<SubscriptionOptions>(),
            sp.GetRequiredService<ISubscriptionDiagnosticEvents>()));
        services.TryAddSingleton<ITopicEventSender>(sp =>
            sp.GetRequiredService<RedisPubSub>());
        services.TryAddSingleton<ITopicEventReceiver>(sp =>
            sp.GetRequiredService<RedisPubSub>());
    }
}
