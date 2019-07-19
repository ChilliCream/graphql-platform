using StackExchange.Redis;

namespace HotChocolate.Subscriptions.Redis
{
    internal static class RedisConnection
    {
        internal static IConnectionMultiplexer Create(
            RedisOptions options)
        {
            var configurationOptions = new ConfigurationOptions
            {
                AbortOnConnectFail = false,
                ConnectRetry = 3,
                ChannelPrefix = "HC_",
                ClientName = options.ClientName,
                Password = options.Password
            };

            foreach (var endpoint in options.Endpoints)
            {
                configurationOptions.EndPoints.Add(endpoint);
            }

            return ConnectionMultiplexer.Connect(
                configurationOptions);
        }
    }
}
