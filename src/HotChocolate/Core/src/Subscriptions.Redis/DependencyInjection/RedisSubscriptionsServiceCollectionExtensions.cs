using System;
using HotChocolate.Execution.Configuration;
using HotChocolate.Subscriptions;
using HotChocolate.Subscriptions.Redis;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RedisSubscriptionsServiceCollectionExtensions
    {
        public static IServiceCollection AddRedisSubscriptions(
            this IServiceCollection services,
            Func<IServiceProvider, IConnectionMultiplexer> connection)
        {
            if (services is null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (connection is null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            services.TryAddSingleton<IMessageSerializer, JsonMessageSerializer>();
            services.TryAddSingleton(sp => new RedisPubSub(
                connection(sp),
                sp.GetRequiredService<IMessageSerializer>()));
            services.TryAddSingleton<ITopicEventSender>(sp =>
                sp.GetRequiredService<RedisPubSub>());
            services.TryAddSingleton<ITopicEventReceiver>(sp =>
                sp.GetRequiredService<RedisPubSub>());
            return services;
        }

        public static IRequestExecutorBuilder AddRedisSubscriptions(
            this IRequestExecutorBuilder builder,
            Func<IServiceProvider, IConnectionMultiplexer> connection)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            if (connection is null)
            {
                throw new ArgumentNullException(nameof(connection));
            }

            AddRedisSubscriptions(builder.Services, connection);
            return builder;
        }
    }
}
