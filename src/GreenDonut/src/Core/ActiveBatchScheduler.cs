using System;

namespace GreenDonut;

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