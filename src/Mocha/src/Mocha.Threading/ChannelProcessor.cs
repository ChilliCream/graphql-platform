namespace Mocha.Threading;

/// <summary>
/// Runs N concurrent <see cref="ContinuousTask"/> workers that consume items from
/// an async source and invoke a handler for each item.
/// </summary>
/// <remarks>
/// The source is a factory that returns an <see cref="IAsyncEnumerable{T}"/> given a
/// cancellation token. Each worker calls the factory independently, so the source must
/// support concurrent readers (e.g. <c>ChannelReader.ReadAllAsync</c> or
/// <c>InMemoryQueue.ConsumeAsync</c>). Disposing the processor disposes all workers.
/// </remarks>
/// <typeparam name="T">The type of item to process.</typeparam>
public sealed class ChannelProcessor<T> : IAsyncDisposable
{
    private readonly ContinuousTask[] _workers;

    /// <summary>
    /// Creates a new channel processor and immediately starts the worker tasks.
    /// </summary>
    /// <param name="source">
    /// A factory that returns an async enumerable of items to process. Called once per worker.
    /// </param>
    /// <param name="handler">The asynchronous delegate invoked for each item.</param>
    /// <param name="concurrency">The number of concurrent worker tasks.</param>
    public ChannelProcessor(Func<CancellationToken, IAsyncEnumerable<T>> source, Func<T, Task> handler, int concurrency)
    {
        _workers = new ContinuousTask[concurrency];
        for (var i = 0; i < concurrency; i++)
        {
            _workers[i] = new ContinuousTask(async ct =>
            {
                await foreach (var item in source(ct))
                {
                    await handler(item);
                }
            });
        }
    }

    /// <summary>
    /// Disposes all worker tasks.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        foreach (var worker in _workers)
        {
            try
            {
                await worker.DisposeAsync();
            }
            catch
            {
                // Best-effort — worker may have faulted.
            }
        }
    }
}
