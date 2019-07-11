using System;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using Subscriptions.Redis;

namespace HotChocolate.Subscriptions
{
    public static class RedisSubscriptionServiceCollectionExtensions
    {
        public static void AddRedisSubscriptionProvider(
            this IServiceCollection services,
            RedisOptions options)
            => services
                .AddRedisSubscriptionProvider<JsonPayloadSerializer>(options);

        public static void AddRedisSubscriptionProvider<TSerializer>(
            this IServiceCollection services,
            RedisOptions options)
            where TSerializer : class, IPayloadSerializer
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services
                .AddSingleton<IPayloadSerializer, TSerializer>()
                .AddSingleton<IConnectionMultiplexer>(sp =>
                    RedisConnection.Create(options))
                .AddSingleton<RedisEventRegistry>()
                .AddSingleton<IEventRegistry>(sp =>
                    sp.GetRequiredService<RedisEventRegistry>())
                .AddSingleton<IEventSender>(sp =>
                    sp.GetRequiredService<RedisEventRegistry>());
        }
    }
}
