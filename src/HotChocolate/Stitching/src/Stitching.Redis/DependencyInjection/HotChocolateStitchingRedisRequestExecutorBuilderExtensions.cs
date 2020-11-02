using System;
using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Stitching.Redis;
using StackExchange.Redis;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HotChocolateStitchingRedisRequestExecutorBuilderExtensions
    {
        public static IRequestExecutorBuilder AddRemoteSchemasFromRedis(
            this IRequestExecutorBuilder builder,
            NameString configurationName,
            Func<IServiceProvider, IConnectionMultiplexer> connectionFactory)
        {
            if (connectionFactory is null)
            {
                throw new ArgumentNullException(nameof(connectionFactory));
            }

            configurationName.EnsureNotEmpty(nameof(configurationName));

            builder.Services.AddSingleton<IRequestExecutorOptionsProvider>(sp =>
            {
                IConnectionMultiplexer connection = connectionFactory(sp);
                IDatabase database = connection.GetDatabase();
                ISubscriber subscriber = connection.GetSubscriber();
                return new RedisExecutorOptionsProvider(
                    builder.Name, configurationName, database, subscriber);
            });

            return builder;
        }
    }
}
