using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Subscriptions
{
    public static class InMemorySubscriptionServiceCollectionExtensions
    {
        public static void AddInMemorySubscriptionProvider(
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
        }
    }
}
