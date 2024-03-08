using System;
using System.Threading.Tasks;

namespace GreenDonut;

/// <summary>
/// The batch scheduler is used by the DataLoader to defer the data fetching
/// work to a batch dispatcher that will execute the batches.
/// </summary>
public interface IBatchScheduler
{
    /// <summary>
    /// Schedules the work that has to be executed to fetch the data.
    /// </summary>
    /// <param name="job">
    /// The work that has to be executed to fetch the data.
    /// </param>
    void Schedule(BatchJob job);
}

public sealed class ActiveBatchScheduler : IBatchScheduler
{
    private IBatchScheduler _activeBatchScheduler = AutoBatchScheduler.Default;
    
    public void Schedule(BatchJob job)
    {
        var batchScheduler = _activeBatchScheduler;
        batchScheduler.Schedule(job);
    }
    
    public void SetActiveScheduler(IBatchScheduler batchScheduler)
    {
        _activeBatchScheduler = batchScheduler ?? 
            throw new ArgumentNullException(nameof(batchScheduler));
    }
} 

public readonly struct BatchJob(Func<ValueTask> batchPromise)
{
    private readonly Func<ValueTask>? _promise = batchPromise;
    
    public ValueTask DispatchAsync() => _promise?.Invoke() ?? default;
}
