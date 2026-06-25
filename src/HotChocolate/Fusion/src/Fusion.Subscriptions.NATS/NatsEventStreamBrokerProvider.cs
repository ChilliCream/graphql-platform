using Microsoft.Extensions.Options;

namespace HotChocolate.Fusion.Subscriptions.NATS;

internal sealed class NatsEventStreamBrokerProvider : IEventStreamBrokerProvider
{
    private readonly NatsEventStreamOptions _options;

    public NatsEventStreamBrokerProvider(
        string name,
        IOptionsMonitor<NatsEventStreamOptions> options)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Get(name);
        Validate(_options);
    }

    public IEventStreamBroker Create()
        => new NatsEventStreamBroker(_options);

    private static void Validate(NatsEventStreamOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Url) && options.ConfigureConnection is null)
        {
            throw new InvalidOperationException(
                "NATS event stream broker options require a URL or connection configuration.");
        }

        if (options.JetStream is { } jetStream)
        {
            if (string.IsNullOrWhiteSpace(jetStream.Stream))
            {
                throw new InvalidOperationException(
                    "NATS JetStream options require a stream name.");
            }

            if (string.IsNullOrWhiteSpace(jetStream.DurableConsumer))
            {
                throw new InvalidOperationException(
                    "NATS JetStream options require a durable consumer name.");
            }
        }
    }
}
