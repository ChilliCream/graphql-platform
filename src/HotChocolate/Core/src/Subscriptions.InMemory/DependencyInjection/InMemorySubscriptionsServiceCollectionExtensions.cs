using System;
using HotChocolate.Execution.Configuration;
using HotChocolate.Subscriptions;
using HotChocolate.Subscriptions.InMemory;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

public static class InMemorySubscriptionsServiceCollectionExtensions
{
    public static IServiceCollection AddInMemorySubscriptions(
        this IServiceCollection services,
        SubscriptionOptions? options = null)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.TryAddSingleton(options ?? new SubscriptionOptions());
        services.TryAddSingleton<InMemoryPubSub>();
        services.TryAddSingleton<ITopicEventSender>(
            sp => sp.GetRequiredService<InMemoryPubSub>());
        services.TryAddSingleton<ITopicEventReceiver>(
            sp => sp.GetRequiredService<InMemoryPubSub>());
        return services;
    }

    public static IRequestExecutorBuilder AddInMemorySubscriptions(
        this IRequestExecutorBuilder builder)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        AddInMemorySubscriptions(builder.Services);
        return builder;
    }
}
