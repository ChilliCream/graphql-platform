using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha.Features;
using Mocha.Transport.Kafka.Connection;
using Mocha.Transport.Kafka.Features;

namespace Mocha.Transport.Kafka;

/// <summary>
/// Kafka receive endpoint that consumes messages from a specific topic using a dedicated consumer.
/// </summary>
/// <param name="transport">The owning Kafka transport instance.</param>
public sealed class KafkaReceiveEndpoint(KafkaMessagingTransport transport)
    : ReceiveEndpoint<KafkaReceiveEndpointConfiguration>(transport)
{
    private IConsumer<byte[], byte[]>? _consumer;
    private CancellationTokenSource? _cts;
    private Task? _consumeLoopTask;
    private ILogger _logger = null!;

    /// <summary>
    /// Gets the Kafka topic that this endpoint consumes from.
    /// </summary>
    public KafkaTopic Topic { get; private set; } = null!;

    /// <summary>
    /// Gets the consumer group identifier for this endpoint.
    /// </summary>
    public string ConsumerGroupId { get; private set; } = null!;

    protected override void OnInitialize(
        IMessagingConfigurationContext context,
        KafkaReceiveEndpointConfiguration configuration)
    {
        if (configuration.TopicName is null)
        {
            throw new InvalidOperationException("Topic name is required");
        }

        ConsumerGroupId = configuration.ConsumerGroupId ?? configuration.TopicName;
    }

    protected override void OnComplete(
        IMessagingConfigurationContext context,
        KafkaReceiveEndpointConfiguration configuration)
    {
        var topology = (KafkaMessagingTopology)Transport.Topology;

        Topic = topology.Topics.FirstOrDefault(t => t.Name == configuration.TopicName)
            ?? throw new InvalidOperationException($"Topic '{configuration.TopicName}' not found");

        Source = Topic;
    }

    protected override ValueTask OnStartAsync(
        IMessagingRuntimeContext context,
        CancellationToken cancellationToken)
    {
        if (Transport is not KafkaMessagingTransport kafkaTransport)
        {
            throw new InvalidOperationException("Transport is not a KafkaMessagingTransport");
        }

        _logger = context.Services.GetRequiredService<ILogger<KafkaReceiveEndpoint>>();
        _cts = new CancellationTokenSource();
        _consumer = kafkaTransport.ConnectionManager.CreateConsumer(ConsumerGroupId, _logger);
        _consumer.Subscribe(Topic.Name);

        _consumeLoopTask = Task.Factory.StartNew(
            () => ConsumeLoopAsync(_consumer, _cts.Token),
            CancellationToken.None,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default).Unwrap();

        return ValueTask.CompletedTask;
    }

    private async Task ConsumeLoopAsync(
        IConsumer<byte[], byte[]> consumer,
        CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ConsumeResult<byte[], byte[]>? result;
                try
                {
                    result = consumer.Consume(cancellationToken);
                }
                catch (ConsumeException ex)
                {
                    // Log and continue -- transient errors should not kill the loop
                    _logger.KafkaConsumeError(ConsumerGroupId, ex.Error.Reason);
                    continue;
                }

                if (result is null)
                {
                    continue;
                }

                try
                {
                    await ExecuteAsync(
                        static (context, state) =>
                        {
                            var feature = context.Features.GetOrSet<KafkaReceiveFeature>();
                            feature.ConsumeResult = state.result;
                            feature.Consumer = state.consumer;
                        },
                        (result, consumer),
                        cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.KafkaConsumeLoopError(ConsumerGroupId, ex);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }
    }

    protected override async ValueTask OnStopAsync(
        IMessagingRuntimeContext context,
        CancellationToken cancellationToken)
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync();
        }

        if (_consumeLoopTask is not null)
        {
            try
            {
                await _consumeLoopTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }

        if (_consumer is not null)
        {
            _consumer.Close();
            _consumer.Dispose();
            _consumer = null;
        }

        _cts?.Dispose();
        _cts = null;
    }
}

internal static partial class KafkaReceiveEndpointLogs
{
    [LoggerMessage(LogLevel.Error, "Kafka consume error in group {GroupId}: {Reason}")]
    public static partial void KafkaConsumeError(this ILogger logger, string groupId, string reason);

    [LoggerMessage(LogLevel.Critical, "Unexpected error processing message in group {GroupId}")]
    public static partial void KafkaConsumeLoopError(this ILogger logger, string groupId, Exception exception);
}
