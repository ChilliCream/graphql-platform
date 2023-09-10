using System;
using HotChocolate;
using HotChocolate.Stitching.Redis;
using HotChocolate.Stitching.SchemaDefinitions;
using HotChocolate.Utilities;
using StackExchange.Redis;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class HotChocolateStitchingRedisPublishSchemaDefinitionDescriptorExtensions
    {
        public static IPublishSchemaDefinitionDescriptor PublishToRedis(
            this IPublishSchemaDefinitionDescriptor descriptor,
            string configurationName,
            Func<IServiceProvider, IConnectionMultiplexer> connectionFactory)
        {
            if (connectionFactory is null)
            {
                throw new ArgumentNullException(nameof(connectionFactory));
            }

            configurationName.EnsureGraphQLName(nameof(configurationName));

            return descriptor.SetSchemaDefinitionPublisher(sp =>
            {
                var connection = connectionFactory(sp);
                return new RedisSchemaDefinitionPublisher(configurationName, connection);
            });
        }
    }
}
