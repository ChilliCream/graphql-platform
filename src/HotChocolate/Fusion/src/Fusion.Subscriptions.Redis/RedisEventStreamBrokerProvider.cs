using Microsoft.Extensions.Options;

namespace HotChocolate.Fusion.Subscriptions.Redis;

internal sealed class RedisEventStreamBrokerProvider : IEventStreamBrokerProvider
{
    private readonly RedisEventStreamOptions _options;

    public RedisEventStreamBrokerProvider(
        string name,
        IOptionsMonitor<RedisEventStreamOptions> options)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Get(name);
        Validate(_options);
    }

    public IEventStreamBroker Create()
        => new RedisEventStreamBroker(_options);

    private static void Validate(RedisEventStreamOptions options)
    {
        if (options.ConnectionMultiplexer is not null)
        {
            return;
        }

        if (options.ConfigurationOptions is not null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(options.Configuration))
        {
            return;
        }

        throw new InvalidOperationException(
            "Redis event stream broker options require a connection multiplexer, a configuration string, or configuration options.");
    }
}
