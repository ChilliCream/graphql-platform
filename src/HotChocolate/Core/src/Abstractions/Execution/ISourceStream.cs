namespace HotChocolate.Execution;

/// <summary>
/// The source stream represents a stream of events from a pub/sub system.
/// </summary>
public interface ISourceStream : IAsyncDisposable
{
    /// <summary>
    /// Reads the subscription result from the pub/sub system.
    /// </summary>
    IAsyncEnumerable<object?> ReadEventsAsync();
}
