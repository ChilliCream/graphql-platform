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
            Func<IServiceProvider, ConfigurationOptions> optionsFactory) =>
            services.AddRedisSubscriptionProvider<JsonPayloadSerializer>(optionsFactory);

        public static IServiceCollection AddRedisSubscriptionProvider(
            this IServiceCollection services,
            Func<IServiceProvider, IConnectionMultiplexer> connectionFactory) =>
            services.AddRedisSubscriptionProvider<JsonPayloadSerializer>(connectionFactory);

        public static IServiceCollection AddRedisSubscriptionProvider<TSerializer>(
            this IServiceCollection services,
            Func<IServiceProvider, ConfigurationOptions> optionsFactory)
            where TSerializer : class, IPayloadSerializer
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton<IConnectionMultiplexer>(sp =>
                ConnectionMultiplexer.Connect(optionsFactory(sp)));
            AddServices<TSerializer>(services);
            return services;
        }

        public static IServiceCollection AddRedisSubscriptionProvider<TSerializer>(
            this IServiceCollection services,
            Func<IServiceProvider, IConnectionMultiplexer> connectionFactory)
            where TSerializer : class, IPayloadSerializer
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddSingleton<IConnectionMultiplexer>(connectionFactory);
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
