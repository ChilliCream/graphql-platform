namespace Mocha.Transport.AzureEventHub;

/// <summary>
/// Specifies the dispatch batching strategy for an Event Hub dispatch endpoint.
/// </summary>
public enum EventHubBatchMode
{
    /// <summary>
    /// Each message is sent individually via <c>SendAsync</c>. This is the default behavior.
    /// </summary>
    Single,

    /// <summary>
    /// Messages are accumulated into <c>EventDataBatch</c> instances and sent as batches
    /// for higher throughput.
    /// </summary>
    Batch
}
