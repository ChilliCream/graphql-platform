using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace HotChocolate.Fusion.Subscriptions.Kafka;

internal sealed class KafkaEventStreamBrokerProvider : IEventStreamBrokerProvider
{
    private readonly KafkaEventStreamOptions _options;

    public KafkaEventStreamBrokerProvider(
        string name,
        IOptionsMonitor<KafkaEventStreamOptions> options)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Get(name);
        Validate(_options);
    }

    public IEventStreamBroker Create()
        => new KafkaEventStreamBroker(_options);

    private static void Validate(KafkaEventStreamOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.BootstrapServers))
        {
            throw new InvalidOperationException(
                "Kafka event stream broker options require bootstrap servers.");
        }

        if (string.IsNullOrWhiteSpace(options.GroupIdPrefix))
        {
            throw new InvalidOperationException(
                "Kafka event stream broker options require a group id prefix.");
        }

        if (options.ConsumerQueuedMinMessages <= 0)
        {
            throw new InvalidOperationException(
                "Kafka event stream broker options require a positive queued message target.");
        }

        if (options.ConsumerQueuedMaxMessagesKbytes <= 0)
        {
            throw new InvalidOperationException(
                "Kafka event stream broker options require a positive queued message byte limit.");
        }

        if (options.ConsumerFetchQueueBackoffMs < 0)
        {
            throw new InvalidOperationException(
                "Kafka event stream broker options require a non-negative fetch queue backoff.");
        }

        if (options.SeedingQueryTimeout <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                "Kafka event stream broker options require a positive seeding query timeout.");
        }

        if (options.SeedingDeadline <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                "Kafka event stream broker options require a positive seeding deadline.");
        }

        if (options.SeedingDeadline < options.SeedingQueryTimeout)
        {
            throw new InvalidOperationException(
                "Kafka event stream broker options require a seeding deadline that is at least the seeding query timeout.");
        }

        if (options.SecurityProtocol is
            SecurityProtocol.SaslPlaintext or
            SecurityProtocol.SaslSsl)
        {
            if (options.SaslMechanism is null)
            {
                throw new InvalidOperationException(
                    "Kafka SASL options require a SASL mechanism.");
            }

            if (string.IsNullOrWhiteSpace(options.SaslUsername))
            {
                throw new InvalidOperationException(
                    "Kafka SASL options require a user name.");
            }

            if (string.IsNullOrWhiteSpace(options.SaslPassword))
            {
                throw new InvalidOperationException(
                    "Kafka SASL options require a password.");
            }
        }
    }
}
