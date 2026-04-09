using System.Collections.Concurrent;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Logging;

namespace Mocha.Transport.Kafka.Connection;

/// <summary>
/// Owns the lifecycle of the shared Kafka producer, per-endpoint consumers, and the admin client
/// used for topology provisioning.
/// </summary>
public sealed class KafkaConnectionManager : IAsyncDisposable
{
    private readonly ILogger<KafkaConnectionManager> _logger;
    private readonly string _bootstrapServers;
    private readonly Action<ProducerConfig>? _producerConfigOverrides;
    private readonly Action<ConsumerConfig>? _consumerConfigOverrides;

    private volatile IProducer<byte[], byte[]>? _producer;
    private volatile IAdminClient? _adminClient;
    private readonly object _producerLock = new();
    private readonly ConcurrentDictionary<TaskCompletionSource, byte> _inflightDispatches = new();
    private bool _isDisposed;

    /// <summary>
    /// Creates a new Kafka connection manager with the specified configuration.
    /// </summary>
    /// <param name="logger">The logger for Kafka lifecycle events.</param>
    /// <param name="bootstrapServers">Comma-separated list of bootstrap servers.</param>
    /// <param name="producerConfigOverrides">Optional overrides applied after default producer configuration.</param>
    /// <param name="consumerConfigOverrides">Optional overrides applied after default consumer configuration.</param>
    public KafkaConnectionManager(
        ILogger<KafkaConnectionManager> logger,
        string bootstrapServers,
        Action<ProducerConfig>? producerConfigOverrides,
        Action<ConsumerConfig>? consumerConfigOverrides)
    {
        _logger = logger;
        _bootstrapServers = bootstrapServers;
        _producerConfigOverrides = producerConfigOverrides;
        _consumerConfigOverrides = consumerConfigOverrides;
    }

    /// <summary>
    /// Gets the shared producer instance. Throws if the producer has not been created.
    /// </summary>
    public IProducer<byte[], byte[]> Producer
        => _producer ?? throw new InvalidOperationException("Producer not created");

    /// <summary>
    /// Creates the shared producer if it does not already exist, using double-checked locking.
    /// </summary>
    public void EnsureProducerCreated()
    {
        if (_producer is not null)
        {
            return;
        }

        lock (_producerLock)
        {
            if (_producer is not null)
            {
                return;
            }

            var config = new ProducerConfig
            {
                BootstrapServers = _bootstrapServers,
                LingerMs = 5,
                BatchNumMessages = 10000,
                Acks = Acks.All,
                EnableIdempotence = true,
                EnableDeliveryReports = true
            };

            _producerConfigOverrides?.Invoke(config);

            _producer = new ProducerBuilder<byte[], byte[]>(config)
                .SetKeySerializer(Serializers.ByteArray)
                .SetValueSerializer(Serializers.ByteArray)
                .SetErrorHandler((_, e) => _logger.KafkaProducerError(e.Reason))
                .SetLogHandler((_, msg) => _logger.KafkaProducerLog(msg.Message))
                .Build();
        }
    }

    /// <summary>
    /// Creates a new consumer for a specific consumer group. Each receive endpoint gets its own consumer.
    /// </summary>
    /// <param name="groupId">The consumer group identifier.</param>
    /// <param name="logger">The logger for consumer lifecycle events.</param>
    /// <returns>A new consumer instance bound to the specified group.</returns>
    public IConsumer<byte[], byte[]> CreateConsumer(
        string groupId,
        ILogger logger)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = groupId,
            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnablePartitionEof = false,
            MaxPollIntervalMs = 600_000 // 10 minutes
        };

        _consumerConfigOverrides?.Invoke(config);

        return new ConsumerBuilder<byte[], byte[]>(config)
            .SetKeyDeserializer(Deserializers.ByteArray)
            .SetValueDeserializer(Deserializers.ByteArray)
            .SetErrorHandler((_, e) => logger.KafkaConsumerError(groupId, e.Reason))
            .SetLogHandler((_, msg) => logger.KafkaConsumerLog(groupId, msg.Message))
            .SetPartitionsAssignedHandler((consumer, partitions) =>
                logger.KafkaPartitionsAssigned(groupId, partitions))
            .SetPartitionsRevokedHandler((consumer, partitions) =>
            {
                // No special action needed: processing is sequential, so there are no
                // in-flight messages from revoked partitions when this handler fires.
                logger.KafkaPartitionsRevoked(groupId, partitions);
            })
            .Build();
    }

    /// <summary>
    /// Gets or creates the shared admin client for topology provisioning.
    /// </summary>
    /// <returns>The admin client instance.</returns>
    public IAdminClient GetOrCreateAdminClient()
    {
        if (_adminClient is not null)
        {
            return _adminClient;
        }

        lock (_producerLock)
        {
            _adminClient ??= new AdminClientBuilder(
                new AdminClientConfig { BootstrapServers = _bootstrapServers })
                .Build();
        }

        return _adminClient;
    }

    /// <summary>
    /// Provisions the specified topics on the Kafka cluster, ignoring errors for topics that already exist.
    /// </summary>
    /// <param name="topics">The topics to create.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    public async Task ProvisionTopologyAsync(
        IEnumerable<KafkaTopic> topics,
        CancellationToken cancellationToken)
    {
        var adminClient = GetOrCreateAdminClient();
        var specs = topics.Select(t => new TopicSpecification
        {
            Name = t.Name,
            NumPartitions = t.Partitions,
            ReplicationFactor = t.ReplicationFactor,
            Configs = t.TopicConfigs
        }).ToList();

        if (specs.Count == 0)
        {
            return;
        }

        try
        {
            await adminClient.CreateTopicsAsync(specs);
        }
        catch (CreateTopicsException ex)
        {
            // Ignore TopicAlreadyExists errors; Results may include
            // successful entries (NoError) alongside the failures.
            foreach (var result in ex.Results)
            {
                if (result.Error.Code != ErrorCode.TopicAlreadyExists
                    && result.Error.Code != ErrorCode.NoError)
                {
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// Tracks an in-flight dispatch operation for graceful shutdown.
    /// </summary>
    /// <param name="tcs">The task completion source representing the in-flight dispatch.</param>
    public void TrackInflight(TaskCompletionSource tcs) => _inflightDispatches.TryAdd(tcs, 0);

    /// <summary>
    /// Untracks an in-flight dispatch operation after delivery report or cancellation.
    /// </summary>
    /// <param name="tcs">The task completion source to untrack.</param>
    public void UntrackInflight(TaskCompletionSource tcs) => _inflightDispatches.TryRemove(tcs, out _);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        if (_producer is not null)
        {
            // Flush pending messages before disposing
            _producer.Flush(TimeSpan.FromSeconds(10));
            _producer.Dispose();
        }

        // Cancel any remaining in-flight dispatch TCS instances.
        // After Flush(10s), any TCS still pending means the delivery
        // report never arrived -- cancel to unblock callers.
        foreach (var tcs in _inflightDispatches.Keys)
        {
            tcs.TrySetCanceled();
        }

        _inflightDispatches.Clear();

        _adminClient?.Dispose();
    }
}

internal static partial class KafkaConnectionManagerLogs
{
    [LoggerMessage(LogLevel.Error, "Kafka producer error: {Reason}")]
    public static partial void KafkaProducerError(this ILogger logger, string reason);

    [LoggerMessage(LogLevel.Debug, "Kafka producer log: {Message}")]
    public static partial void KafkaProducerLog(this ILogger logger, string message);

    [LoggerMessage(LogLevel.Error, "Kafka consumer error in group {GroupId}: {Reason}")]
    public static partial void KafkaConsumerError(this ILogger logger, string groupId, string reason);

    [LoggerMessage(LogLevel.Debug, "Kafka consumer log in group {GroupId}: {Message}")]
    public static partial void KafkaConsumerLog(this ILogger logger, string groupId, string message);

    [LoggerMessage(LogLevel.Information, "Kafka partitions assigned for group {GroupId}: {Partitions}")]
    public static partial void KafkaPartitionsAssigned(
        this ILogger logger,
        string groupId,
        List<TopicPartition> partitions);

    [LoggerMessage(LogLevel.Information, "Kafka partitions revoked for group {GroupId}: {Partitions}")]
    public static partial void KafkaPartitionsRevoked(
        this ILogger logger,
        string groupId,
        List<TopicPartitionOffset> partitions);
}
