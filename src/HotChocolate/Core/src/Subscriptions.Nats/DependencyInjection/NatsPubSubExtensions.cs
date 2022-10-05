using AlterNats;
using HotChocolate.Subscriptions;
using HotChocolate.Subscriptions.Nats;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class NatsPubSubExtensions
{
    /// <summary>
    /// Enables support for using NATS as a subscription provider. Ensure you have configured the NATS client before calling this method.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddNatsSubscriptions(this IServiceCollection services)
    {
        services.TryAddSingleton<NatsPubSub>();
        services.TryAddSingleton<ITopicEventSender>(sp => sp.GetRequiredService<NatsPubSub>());
        services.TryAddSingleton<ITopicEventReceiver>(sp => sp.GetRequiredService<NatsPubSub>());

        return services;
    }
}
