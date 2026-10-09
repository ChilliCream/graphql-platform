using Azure.Messaging.EventHubs;
using Azure.Messaging.EventHubs.Consumer;

namespace HotChocolate.Fusion.Subscriptions.AzureEventHubs;

internal sealed class EventHubsPartitionSource(IReadOnlyDictionary<string, EventHubConsumerClient> clients)
    : IPartitionIdsSource,
      IPartitionPropertiesSource
{
    public Task<string[]> GetPartitionIdsAsync(string hub, CancellationToken cancellationToken)
        => clients[hub].GetPartitionIdsAsync(cancellationToken);

    public Task<PartitionProperties> GetPartitionPropertiesAsync(
        string hub,
        string partitionId,
        CancellationToken cancellationToken)
        => clients[hub].GetPartitionPropertiesAsync(partitionId, cancellationToken);
}
