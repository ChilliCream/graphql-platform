using Azure.Messaging.EventHubs;

namespace HotChocolate.Fusion.Subscriptions.AzureEventHubs;

internal interface IPartitionPropertiesSource
{
    Task<PartitionProperties> GetPartitionPropertiesAsync(
        string hub,
        string partitionId,
        CancellationToken cancellationToken);
}
