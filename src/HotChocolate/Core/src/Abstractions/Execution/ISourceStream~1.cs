namespace HotChocolate.Execution;

/// <summary>
/// The source stream represents a stream of events from a pub/sub system.
/// </summary>
public interface ISourceStream<out TMessage> : ISourceStream
{
    /// <summary>
    /// Reads the subscription result from the pub/sub system.
    /// </summary>
    new IAsyncEnumerable<TMessage> ReadEventsAsync();
}
