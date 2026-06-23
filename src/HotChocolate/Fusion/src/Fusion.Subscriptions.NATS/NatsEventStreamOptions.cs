using NATS.Client.Core;

namespace HotChocolate.Fusion.Subscriptions.NATS;

public sealed class NatsEventStreamOptions
{
    public string? Url { get; set; }

    public Func<NatsOpts, NatsOpts>? ConfigureConnection { get; set; }

    public NatsJetStreamOptions? JetStream { get; set; }
}

public sealed class NatsJetStreamOptions
{
    public required string Stream { get; set; }

    public required string DurableConsumer { get; set; }
}
