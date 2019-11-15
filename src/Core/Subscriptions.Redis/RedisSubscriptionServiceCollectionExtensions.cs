using System;
using HotChocolate.Subscriptions.Redis;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace HotChocolate.Subscriptions
{
    public static class RedisSubscriptionServiceCollectionExtensions
    {
        public static IServiceCollection AddRedisSubscriptionProvider(
            this IServiceCollection services,
            ConfigurationOptions options) =>
            services.AddRedisSubscriptionProvider<JsonPayloadSerializer>(options);

        public static IServiceCollection AddRedisSubscriptionProvider(
            this IServiceCollection services,
            ConnectionMultiplexer connection) =>
            services.AddRedisSubscriptionProvider<JsonPayloadSerializer>(connection);

        public static IServiceCollection AddRedisSubscriptionProvider<TSerializer>(
            this IServiceCollection services,
            ConfigurationOptions options)
            where TSerializer : class, IPayloadSerializer
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton<IConnectionMultiplexer>(sp =>
                ConnectionMultiplexer.Connect(options));
            AddServices<TSerializer>(services);
            return services;
        }

        public static IServiceCollection AddRedisSubscriptionProvider<TSerializer>(
            this IServiceCollection services,
            ConnectionMultiplexer connection)
            where TSerializer : class, IPayloadSerializer
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton<IConnectionMultiplexer>(sp => connection);
            AddServices<TSerializer>(services);
            return services;
        }

        private static IServiceCollection AddServices<TSerializer>(
            IServiceCollection services)
            where TSerializer : class, IPayloadSerializer
        {
            services
                .AddSingleton<IPayloadSerializer, TSerializer>()
                .AddSingleton<RedisEventRegistry>()
                .AddSingleton<IEventRegistry>(sp =>
                    sp.GetRequiredService<RedisEventRegistry>())
                .AddSingleton<IEventSender>(sp =>
                    sp.GetRequiredService<RedisEventRegistry>());
            return services;
        }
    }
}
