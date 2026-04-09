using Azure.Messaging.EventHubs.Primitives;

namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Pluggable store for coordinating partition ownership across multiple processor instances.
/// Phase 1 uses single-instance mode (claim all, list empty); future implementations can use
/// Blob Storage or a database for distributed partition balancing.
/// </summary>
public interface IPartitionOwnershipStore
{
    /// <summary>
    /// Lists all current partition ownership records for the specified Event Hub and consumer group.
    /// </summary>
    /// <param name="fullyQualifiedNamespace">The fully qualified Event Hubs namespace.</param>
    /// <param name="eventHubName">The name of the Event Hub.</param>
    /// <param name="consumerGroup">The consumer group name.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The current ownership records.</returns>
    ValueTask<IEnumerable<EventProcessorPartitionOwnership>> ListOwnershipAsync(
        string fullyQualifiedNamespace,
        string eventHubName,
        string consumerGroup,
        CancellationToken cancellationToken);

    /// <summary>
    /// Attempts to claim ownership of the specified partitions using optimistic concurrency.
    /// Only partitions that were successfully claimed are included in the result.
    /// </summary>
    /// <param name="desiredOwnership">The ownership records to claim.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The ownership records that were successfully claimed.</returns>
    ValueTask<IEnumerable<EventProcessorPartitionOwnership>> ClaimOwnershipAsync(
        IEnumerable<EventProcessorPartitionOwnership> desiredOwnership,
        CancellationToken cancellationToken);
}
