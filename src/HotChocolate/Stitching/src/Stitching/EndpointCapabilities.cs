namespace HotChocolate.Stitching;

public struct EndpointCapabilities
{
    public BatchingSupport Batching { get; set; }

    public SubscriptionSupport Subscriptions { get; set; }
}
