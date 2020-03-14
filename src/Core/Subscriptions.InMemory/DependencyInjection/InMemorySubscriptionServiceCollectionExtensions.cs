using System;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Subscriptions;
using HotChocolate.Subscriptions.InMemory;

namespace HotChocolate
{
    public static class InMemoryPubSubServiceCollectionExtensions
    {
        public static IServiceCollection AddInMemorySubscriptions(
            this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton<InMemoryPubSub>();
            services.AddSingleton<ITopicEventSender>(sp =>
                sp.GetRequiredService<InMemoryPubSub>());
            services.AddSingleton<ITopicEventReceiver>(sp =>
                sp.GetRequiredService<InMemoryPubSub>());
            return services;
        }
    }
}

