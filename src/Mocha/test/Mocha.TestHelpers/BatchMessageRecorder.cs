using System.Collections.Concurrent;

namespace Mocha.TestHelpers;

/// <summary>
/// Thread-safe recorder for batch handler invocations in integration tests.
/// </summary>
public sealed class BatchMessageRecorder
{
    private readonly SemaphoreSlim _semaphore = new(0);
    private readonly ConcurrentBag<object> _batches = [];

    public IReadOnlyCollection<object> Batches => _batches;

    public void Record<TEvent>(IMessageBatch<TEvent> batch)
    {
        _batches.Add(batch);
        _semaphore.Release();
    }

    public async Task<bool> WaitAsync(TimeSpan timeout, int expectedCount = 1)
    {
        for (var i = 0; i < expectedCount; i++)
        {
            if (!await _semaphore.WaitAsync(timeout))
            {
                return false;
            }
        }

        return true;
    }
}
