using System;
using HotChocolate.Subscriptions.Redis;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace HotChocolate.Subscriptions
{
    public static class RedisSubscriptionServiceCollectionExtensions
    {
        public static IServiceCollection AddRedisSubscriptions(
            this IServiceCollection services,
            ConfigurationOptions options)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton<IConnectionMultiplexer>(sp =>
                ConnectionMultiplexer.Connect(options));
            AddServices(services);
            return services;
        }

        public static IServiceCollection AddRedisSubscriptions(
            this IServiceCollection services,
            ConnectionMultiplexer connection)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton<IConnectionMultiplexer>(sp => connection);
            AddServices(services);
            return services;
        }

        private static IServiceCollection AddServices(
            IServiceCollection services)
        {
            services
                .AddSingleton<RedisPubSub>()
                .AddSingleton<ITopicEventSender>(sp =>
                    sp.GetRequiredService<RedisPubSub>())
                .AddSingleton<ITopicEventReceiver>(sp =>
                    sp.GetRequiredService<RedisPubSub>());
            return services;
        }
    }
}
