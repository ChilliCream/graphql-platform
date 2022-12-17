using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using HotChocolate.Subscriptions;
using HotChocolate.Subscriptions.Nats;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// These helper methods allows to register the NATS subscription
/// provider with the GraphQL configuration.
/// </summary>
public static class NatsPubSubExtensions
{
    /// <summary>
    /// Adds support for using NATS as a subscription provider.
    /// Ensure you have configured the NATS client using <code>AddNats(...)</code>
    /// before calling this method.
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
    public static IRequestExecutorBuilder AddNatsSubscriptions(
        this IRequestExecutorBuilder builder,
        SubscriptionOptions? options = null)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.AddSubscriptionDiagnostics();
        AddNatsSubscriptions(builder.Services, options);
        return builder;
    }

    private static void AddNatsSubscriptions(
        this IServiceCollection services,
        SubscriptionOptions? options = null)
    {
        services.TryAddSingleton(options ?? new SubscriptionOptions());
        services.TryAddSingleton<IMessageSerializer, DefaultJsonMessageSerializer>();
        services.TryAddSingleton<NatsPubSub>();
        services.TryAddSingleton<ITopicEventSender>(sp => sp.GetRequiredService<NatsPubSub>());
        services.TryAddSingleton<ITopicEventReceiver>(sp => sp.GetRequiredService<NatsPubSub>());
    }
}
