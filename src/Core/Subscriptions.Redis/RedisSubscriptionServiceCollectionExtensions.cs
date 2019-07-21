using System;
using HotChocolate.Subscriptions.Redis;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace HotChocolate.Subscriptions
{
    public static class RedisSubscriptionServiceCollectionExtensions
    {
        public static void AddRedisSubscriptionProvider(
            this IServiceCollection services,
            ConfigurationOptions options) =>
            services
                .AddRedisSubscriptionProvider<JsonPayloadSerializer>(options);

        public static void AddRedisSubscriptionProvider<TSerializer>(
            this IServiceCollection services,
            ConfigurationOptions options)
            where TSerializer : class, IPayloadSerializer
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services
                .AddSingleton<IPayloadSerializer, TSerializer>()
                .AddSingleton<IConnectionMultiplexer>(sp =>
                    ConnectionMultiplexer.Connect(options))
                .AddSingleton<RedisEventRegistry>()
                .AddSingleton<IEventRegistry>(sp =>
                    sp.GetRequiredService<RedisEventRegistry>())
                .AddSingleton<IEventSender>(sp =>
                    sp.GetRequiredService<RedisEventRegistry>());
        }
    }
}
