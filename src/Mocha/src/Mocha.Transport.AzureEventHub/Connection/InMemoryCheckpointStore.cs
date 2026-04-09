using System.Collections.Concurrent;

namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Tracks last processed sequence number per partition in memory.
/// Provides at-least-once delivery within a process lifetime. Checkpoints are lost on restart.
/// </summary>
public sealed class InMemoryCheckpointStore : ICheckpointStore
{
    private readonly ConcurrentDictionary<(string, string, string, string), long> _checkpoints = new();

    /// <inheritdoc />
    public ValueTask<long?> GetCheckpointAsync(
        string fullyQualifiedNamespace,
        string eventHubName,
        string consumerGroup,
        string partitionId,
        CancellationToken cancellationToken)
    {
        var key = (fullyQualifiedNamespace, eventHubName, consumerGroup, partitionId);
        return _checkpoints.TryGetValue(key, out var seq)
            ? new ValueTask<long?>(seq)
            : new ValueTask<long?>((long?)null);
    }

    /// <inheritdoc />
    public ValueTask SetCheckpointAsync(
        string fullyQualifiedNamespace,
        string eventHubName,
        string consumerGroup,
        string partitionId,
        long sequenceNumber,
        CancellationToken cancellationToken)
    {
        _checkpoints[(fullyQualifiedNamespace, eventHubName, consumerGroup, partitionId)] = sequenceNumber;
        return default;
    }
}
