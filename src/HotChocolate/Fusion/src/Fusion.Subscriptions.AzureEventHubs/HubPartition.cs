namespace HotChocolate.Fusion.Subscriptions.AzureEventHubs;

internal readonly record struct HubPartition(string Hub, string PartitionId);
