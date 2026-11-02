namespace HotChocolate.Fusion.Subscriptions.AzureEventHubs;

/// <summary>
/// The decoded resume cursor: the next sequence number to read for every observed
/// (hub, partition) pair, plus the set of partition ids each hub had when the cursor
/// was minted. The minted partition ids are used to detect a partition shrink or
/// hub re-creation when a subscription resumes.
/// </summary>
internal sealed class AzureEventHubsResumeState
{
    public required Dictionary<HubPartition, long> NextSequenceNumbers { get; init; }

    public required Dictionary<string, IReadOnlySet<string>> MintedPartitionIds { get; init; }
}
