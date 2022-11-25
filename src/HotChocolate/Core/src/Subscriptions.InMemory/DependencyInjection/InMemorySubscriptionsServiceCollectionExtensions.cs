using HotChocolate.Execution.Configuration;
using HotChocolate.Subscriptions;
using HotChocolate.Subscriptions.InMemory;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// These helper methods allows to register the in-memory subscription
/// provider with the GraphQL configuration.
/// </summary>
public static class InMemorySubscriptionsServiceCollectionExtensions
{
    /// <summary>
    /// Adds the in-memory subscription provider to the GraphQL configuration.
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
    public static IRequestExecutorBuilder AddInMemorySubscriptions(
        this IRequestExecutorBuilder builder,
        SubscriptionOptions? options = null)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.AddSubscriptionDiagnostics();
        AddInMemorySubscriptions(builder.Services, options);
        return builder;
    }

    private static void AddInMemorySubscriptions(
        this IServiceCollection services,
        SubscriptionOptions? options)
    {
        services.TryAddSingleton(options ?? new SubscriptionOptions());
        services.TryAddSingleton<InMemoryPubSub>();
        services.TryAddSingleton<ITopicEventSender>(
            sp => sp.GetRequiredService<InMemoryPubSub>());
        services.TryAddSingleton<ITopicEventReceiver>(
            sp => sp.GetRequiredService<InMemoryPubSub>());
    }
}
