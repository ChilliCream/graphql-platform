namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Pluggable checkpoint store for tracking the last processed sequence number per partition.
/// Phase 1 uses in-memory; future phases can implement Blob Storage, database, etc.
/// </summary>
public interface ICheckpointStore
{
    /// <summary>
    /// Gets the checkpoint (last processed sequence number) for a partition.
    /// </summary>
    /// <param name="fullyQualifiedNamespace">The fully qualified Event Hubs namespace.</param>
    /// <param name="eventHubName">The name of the Event Hub.</param>
    /// <param name="consumerGroup">The consumer group name.</param>
    /// <param name="partitionId">The partition identifier.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The last processed sequence number, or <c>null</c> if no checkpoint exists.</returns>
    ValueTask<long?> GetCheckpointAsync(
        string fullyQualifiedNamespace,
        string eventHubName,
        string consumerGroup,
        string partitionId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates the checkpoint for a partition.
    /// </summary>
    /// <param name="fullyQualifiedNamespace">The fully qualified Event Hubs namespace.</param>
    /// <param name="eventHubName">The name of the Event Hub.</param>
    /// <param name="consumerGroup">The consumer group name.</param>
    /// <param name="partitionId">The partition identifier.</param>
    /// <param name="sequenceNumber">The sequence number to checkpoint.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    ValueTask SetCheckpointAsync(
        string fullyQualifiedNamespace,
        string eventHubName,
        string consumerGroup,
        string partitionId,
        long sequenceNumber,
        CancellationToken cancellationToken);
}
