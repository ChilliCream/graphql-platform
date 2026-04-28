using System.Collections.Concurrent;
using Azure.Core;
using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;
using Azure.Messaging.EventHubs.Primitives;
using Microsoft.Extensions.Logging;

namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Custom <see cref="EventProcessor{TPartition}"/> subclass that provides built-in reconnection,
/// partition load balancing, and pluggable checkpoint store integration with the Mocha receive pipeline.
/// </summary>
internal class MochaEventProcessor : EventProcessor<EventProcessorPartition>
{
    private readonly ILogger _logger;
    private readonly Func<EventData, string, CancellationToken, ValueTask> _messageHandler;
    private readonly ICheckpointStore _checkpointStore;
    private readonly IPartitionOwnershipStore? _ownershipStore;
    private readonly string _fullyQualifiedNamespace;
    private readonly string _eventHubName;
    private readonly string _consumerGroup;
    private readonly int _checkpointInterval;
    private readonly EventPosition _defaultStartingPosition;
    private readonly ConcurrentDictionary<string, int> _partitionCounters = new();
    private readonly ConcurrentDictionary<string, long> _partitionLastSequence = new();
    private readonly ConcurrentDictionary<string, long> _partitionLastCheckpointTime = new();
    private const long CheckpointTimeoutMs = 30_000;

    /// <summary>
    /// Creates a new processor using token credential authentication.
    /// </summary>
    public MochaEventProcessor(
        ILogger logger,
        string consumerGroup,
        string fullyQualifiedNamespace,
        string eventHubName,
        TokenCredential credential,
        Func<EventData, string, CancellationToken, ValueTask> messageHandler,
        ICheckpointStore checkpointStore,
        IPartitionOwnershipStore? ownershipStore,
        int checkpointInterval,
        EventPosition defaultStartingPosition,
        int eventBatchMaximumCount,
        EventProcessorOptions processorOptions)
        : base(
            eventBatchMaximumCount,
            consumerGroup,
            fullyQualifiedNamespace,
            eventHubName,
            credential,
            processorOptions)
    {
        _logger = logger;
        _messageHandler = messageHandler;
        _checkpointStore = checkpointStore;
        _ownershipStore = ownershipStore;
        _fullyQualifiedNamespace = fullyQualifiedNamespace;
        _eventHubName = eventHubName;
        _consumerGroup = consumerGroup;
        _checkpointInterval = checkpointInterval;
        _defaultStartingPosition = defaultStartingPosition;
    }

    /// <summary>
    /// Creates a new processor using connection string authentication.
    /// </summary>
    public MochaEventProcessor(
        ILogger logger,
        string consumerGroup,
        string connectionString,
        string eventHubName,
        Func<EventData, string, CancellationToken, ValueTask> messageHandler,
        ICheckpointStore checkpointStore,
        IPartitionOwnershipStore? ownershipStore,
        int checkpointInterval,
        EventPosition defaultStartingPosition,
        int eventBatchMaximumCount,
        EventProcessorOptions processorOptions)
        : base(
            eventBatchMaximumCount,
            consumerGroup,
            connectionString,
            eventHubName,
            processorOptions)
    {
        _logger = logger;
        _messageHandler = messageHandler;
        _checkpointStore = checkpointStore;
        _ownershipStore = ownershipStore;

        var props = EventHubsConnectionStringProperties.Parse(connectionString);
        _fullyQualifiedNamespace = props.FullyQualifiedNamespace;
        _eventHubName = eventHubName;
        _consumerGroup = consumerGroup;
        _checkpointInterval = checkpointInterval;
        _defaultStartingPosition = defaultStartingPosition;
    }

    /// <inheritdoc />
    protected override async Task OnProcessingEventBatchAsync(
        IEnumerable<EventData> events,
        EventProcessorPartition partition,
        CancellationToken cancellationToken)
    {
        long lastSuccessfulSequence = -1;
        var eventList = events as IList<EventData> ?? events.ToList();
        var lastIndex = eventList.Count - 1;
        var index = 0;

        foreach (var eventData in eventList)
        {
            try
            {
                await _messageHandler(eventData, partition.PartitionId, cancellationToken);
                lastSuccessfulSequence = eventData.SequenceNumber;
                _partitionLastSequence[partition.PartitionId] = lastSuccessfulSequence;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.ErrorProcessingEvent(ex, _eventHubName, partition.PartitionId, eventData.SequenceNumber);
                break;
            }

            var counter = _partitionCounters.AddOrUpdate(
                partition.PartitionId, 1, static (_, c) => c + 1);

            var now = Environment.TickCount64;
            var lastTime = _partitionLastCheckpointTime.GetOrAdd(partition.PartitionId, now);

            // Checkpoint when the interval is reached, on the last event in the batch,
            // or when the time-based timeout has elapsed.
            if (counter >= _checkpointInterval || index == lastIndex || (now - lastTime) >= CheckpointTimeoutMs)
            {
                await _checkpointStore.SetCheckpointAsync(
                    _fullyQualifiedNamespace,
                    _eventHubName,
                    _consumerGroup,
                    partition.PartitionId,
                    lastSuccessfulSequence,
                    cancellationToken);

                _partitionCounters[partition.PartitionId] = 0;
                _partitionLastCheckpointTime[partition.PartitionId] = now;
            }

            index++;
        }
    }

    /// <summary>
    /// Flushes any pending checkpoints for all partitions. Called during graceful shutdown
    /// to ensure no processed events are lost.
    /// </summary>
    internal async Task FlushCheckpointsAsync(CancellationToken cancellationToken)
    {
        foreach (var (partitionId, counter) in _partitionCounters)
        {
            if (counter > 0 && _partitionLastSequence.TryGetValue(partitionId, out var seq))
            {
                await _checkpointStore.SetCheckpointAsync(
                    _fullyQualifiedNamespace,
                    _eventHubName,
                    _consumerGroup,
                    partitionId,
                    seq,
                    cancellationToken);
                _partitionCounters[partitionId] = 0;
            }
        }
    }

    /// <inheritdoc />
    protected override Task OnProcessingErrorAsync(
        Exception exception,
        EventProcessorPartition? partition,
        string operationDescription,
        CancellationToken cancellationToken)
    {
        _logger.ErrorProcessingPartition(
            exception,
            _eventHubName,
            partition?.PartitionId ?? "unknown",
            operationDescription);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    protected override async Task<EventProcessorCheckpoint> GetCheckpointAsync(
        string partitionId,
        CancellationToken cancellationToken)
    {
        var sequenceNumber = await _checkpointStore.GetCheckpointAsync(
            _fullyQualifiedNamespace,
            _eventHubName,
            _consumerGroup,
            partitionId,
            cancellationToken);

        var startingPosition = sequenceNumber.HasValue
            ? EventPosition.FromSequenceNumber(sequenceNumber.Value, isInclusive: false)
            : _defaultStartingPosition;

        return new EventProcessorCheckpoint
        {
            FullyQualifiedNamespace = _fullyQualifiedNamespace,
            EventHubName = _eventHubName,
            ConsumerGroup = _consumerGroup,
            PartitionId = partitionId,
            StartingPosition = startingPosition
        };
    }

    /// <inheritdoc />
    protected override async Task<IEnumerable<EventProcessorPartitionOwnership>> ClaimOwnershipAsync(
        IEnumerable<EventProcessorPartitionOwnership> desiredOwnership,
        CancellationToken cancellationToken)
    {
        if (_ownershipStore is not null)
        {
            return await _ownershipStore.ClaimOwnershipAsync(desiredOwnership, cancellationToken);
        }

        // Single-instance mode: accept all claimed partitions.
        return desiredOwnership;
    }

    /// <inheritdoc />
    protected override async Task<IEnumerable<EventProcessorPartitionOwnership>> ListOwnershipAsync(
        CancellationToken cancellationToken)
    {
        if (_ownershipStore is not null)
        {
            return await _ownershipStore.ListOwnershipAsync(
                _fullyQualifiedNamespace,
                _eventHubName,
                _consumerGroup,
                cancellationToken);
        }

        // Single-instance mode: no distributed coordination.
        return Enumerable.Empty<EventProcessorPartitionOwnership>();
    }

    /// <inheritdoc />
    protected override Task<IEnumerable<EventProcessorCheckpoint>> ListCheckpointsAsync(
        CancellationToken cancellationToken)
    {
        // Return empty; use GetCheckpointAsync per-partition instead.
        return Task.FromResult(Enumerable.Empty<EventProcessorCheckpoint>());
    }
}

internal static partial class MochaEventProcessorLogMessages
{
    [LoggerMessage(LogLevel.Error,
        "Error processing Event Hub '{EventHubName}' partition '{PartitionId}' during '{OperationDescription}'")]
    public static partial void ErrorProcessingPartition(
        this ILogger logger,
        Exception exception,
        string eventHubName,
        string partitionId,
        string operationDescription);

    [LoggerMessage(LogLevel.Error,
        "Error processing event from Event Hub '{EventHubName}' partition '{PartitionId}' sequence '{SequenceNumber}'")]
    public static partial void ErrorProcessingEvent(
        this ILogger logger,
        Exception exception,
        string eventHubName,
        string partitionId,
        long sequenceNumber);
}
