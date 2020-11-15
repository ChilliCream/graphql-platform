using System;
using Microsoft.Extensions.DependencyInjection.Extensions;
using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Stitching.Redis;
using HotChocolate.Stitching.Requests;
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

            // Last but not least, we will setup the stitching context which will
            // provide access to the remote executors which in turn use the just configured
            // request executor proxies to send requests to the downstream services.
            builder.Services.TryAddScoped<IStitchingContext, StitchingContext>();

            return builder;
        }
    }
}
