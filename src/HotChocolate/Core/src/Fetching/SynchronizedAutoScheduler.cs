using System;
using System.Threading;
using System.Threading.Tasks;
using GreenDonut;

namespace HotChocolate.Fetching;

public sealed class SynchronizedAutoScheduler : IExecutorBatchScheduler
{
    private const int _waitTimeout = 30_000;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    
    public void RegisterTaskEnqueuedCallback(Action callback)
    {
    }

    public void Schedule(BatchJob job)
        => BeginDispatchOnSchedule(job);

    public void BeginDispatch(CancellationToken cancellationToken = default)
    {
    }
    
#pragma warning disable 4014
    private void BeginDispatchOnSchedule(BatchJob job)
        => DispatchOnScheduleAsync(job);
#pragma warning restore 4014

    private async Task DispatchOnScheduleAsync(BatchJob job)
    {
        await _semaphore.WaitAsync(_waitTimeout).ConfigureAwait(false);

        try
        {
            await job.DispatchAsync().ConfigureAwait(false);
        }
        catch
        {
            // we catch any batching exception here.
            // standard exceptions are handled in the DataLoader itself,
            // exceptions here have to do with cancellations.
        }
        finally
        {
            _semaphore.Release();
        }
    }
    
    public void Dispose()
        => _semaphore.Dispose();
}