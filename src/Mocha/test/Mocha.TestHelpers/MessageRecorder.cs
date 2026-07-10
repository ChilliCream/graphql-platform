using System.Collections.Concurrent;

namespace Mocha.TestHelpers;

public sealed class MessageRecorder
{
    private readonly SemaphoreSlim _semaphore = new(0);

    public ConcurrentBag<object> Messages { get; } = [];

    public void Record(object message)
    {
        Messages.Add(message);
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
