namespace Mocha.TestHelpers;

public sealed class ConcurrencyTracker
{
    private readonly SemaphoreSlim _semaphore = new(0);
    private int _current;
    private int _peak;
    private int _completed;

    public int PeakConcurrency => Volatile.Read(ref _peak);

    public int CurrentConcurrency => Volatile.Read(ref _current);

    public int CompletedCount => Volatile.Read(ref _completed);

    public void Enter()
    {
        var current = Interlocked.Increment(ref _current);
        int oldPeak;
        do
        {
            oldPeak = Volatile.Read(ref _peak);
            if (current <= oldPeak)
            {
                break;
            }
        } while (Interlocked.CompareExchange(ref _peak, current, oldPeak) != oldPeak);
    }

    public void Exit()
    {
        Interlocked.Decrement(ref _current);
        Interlocked.Increment(ref _completed);
        _semaphore.Release();
    }

    public async Task<bool> WaitAsync(TimeSpan timeout, int expectedCount)
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
