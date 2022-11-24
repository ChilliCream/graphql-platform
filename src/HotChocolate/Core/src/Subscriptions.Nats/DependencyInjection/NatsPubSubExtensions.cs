using Microsoft.Extensions.DependencyInjection.Extensions;
using HotChocolate.Subscriptions;
using HotChocolate.Subscriptions.Nats;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static class NatsPubSubExtensions
{
    /// <summary>
    /// Enables support for using NATS as a subscription provider.
    /// Ensure you have configured the NATS client using <code>AddNats(...)</code>
    /// before calling this method.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="prefix">
    /// If you wish to share a NATS instance/cluster with multiple, distinct GraphQL servers,
    /// you must provide a unique subject prefix for this instance, i.e. "dev01".
    /// All servers with the same prefix will be part of the same publish/subscribe group,
    /// and will receive all messages.
    /// The prefix will be prepended to all NATS subjects with the "." subject token separator,
    /// and as such can be monitored with the NATS CLI using the &gt; operator:
    /// i.e. <code>./nats -s localhost:4222 subscribe dev01.&gt;</code>
    ///
    /// If you do not provide a prefix, the server will use the default prefix of "graphql".
    /// Only a-z,A-Z and 0-9 characters are permitted. The prefix is case sensitive. </param>
    /// <param name="options"></param>
    /// <returns></returns>
    public static IServiceCollection AddNatsSubscriptions(
        this IServiceCollection services,
        SubscriptionOptions? options = null)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.TryAddSingleton(_ => options ?? new SubscriptionOptions());
        services.TryAddSingleton<IMessageSerializer, DefaultJsonMessageSerializer>();
        services.TryAddSingleton<NatsPubSub>();
        services.TryAddSingleton<ITopicEventSender>(sp => sp.GetRequiredService<NatsPubSub>());
        services.TryAddSingleton<ITopicEventReceiver>(sp => sp.GetRequiredService<NatsPubSub>());
        return services;
    }
}
