namespace HotChocolate.Stitching;

public struct EndpointCapabilities
{
    public BatchingSupport Batching { get; set; }

    public SubscriptionSupport Subscriptions { get; set; }
}

public enum BatchingSupport
{
    Off = 0,
    RequestBatching = 1,
    OperationBatching = 2
}

public enum SubscriptionSupport
{
    Off = 0,
    WebSocket = 1
}
