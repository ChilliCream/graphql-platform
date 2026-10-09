namespace HotChocolate.Fusion.Subscriptions.AzureEventHubs;

internal interface IPartitionIdsSource
{
    Task<string[]> GetPartitionIdsAsync(string hub, CancellationToken cancellationToken);
}
