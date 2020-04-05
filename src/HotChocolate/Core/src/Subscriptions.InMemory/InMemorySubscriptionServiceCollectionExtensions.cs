using System;
using HotChocolate.Subscriptions;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class InMemorySubscriptionServiceCollectionExtensions
    {
        public static IServiceCollection AddInMemorySubscriptionProvider(
            this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton<InMemoryEventRegistry>();
            services.AddSingleton<IEventRegistry>(sp =>
                sp.GetRequiredService<InMemoryEventRegistry>());
            services.AddSingleton<IEventSender>(sp =>
                sp.GetRequiredService<InMemoryEventRegistry>());
            return services;
        }
    }
}
