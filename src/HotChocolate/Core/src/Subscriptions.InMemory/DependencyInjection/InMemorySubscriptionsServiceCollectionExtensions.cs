using System;
using HotChocolate.Execution.Configuration;
using HotChocolate.Subscriptions;
using HotChocolate.Subscriptions.InMemory;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class InMemorySubscriptionsServiceCollectionExtensions
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

        public static IRequestExecutorBuilder AddInMemorySubscriptions(
            this IRequestExecutorBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            AddInMemorySubscriptions(builder.Services);
            return builder;
        }
    }
}
