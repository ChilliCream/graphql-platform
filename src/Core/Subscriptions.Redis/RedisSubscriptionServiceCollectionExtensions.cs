using System;
using Microsoft.Extensions.DependencyInjection;
using Subscriptions.Redis;

namespace HotChocolate.Subscriptions
{
    public static class RedisSubscriptionServiceCollectionExtensions
    {
        public static void AddRedisSubscriptionProvider<TSerializer>(
            this IServiceCollection services)
            where TSerializer : class, IPayloadSerializer
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton<IPayloadSerializer, TSerializer>();
            services.AddSingleton<RedisEventRegistry>();
            services.AddSingleton<IEventRegistry>(sp =>
                sp.GetRequiredService<RedisEventRegistry>());
            services.AddSingleton<IEventSender>(sp =>
                sp.GetRequiredService<RedisEventRegistry>());
        }

        public static void AddRedisSubscriptionProvider(
            this IServiceCollection services)
        {
            services.AddRedisSubscriptionProvider<JsonPayloadSerializer>();
        }
    }
}
