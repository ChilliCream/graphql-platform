using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Fetching;

namespace HotChocolate.Execution.Processing;

#pragma warning disable CS0067
internal sealed class NoopBatchDispatcher : IBatchDispatcher
{
    public event EventHandler? TaskEnqueued;

    public IExecutionTaskScheduler Scheduler => DefaultExecutionTaskScheduler.Instance;

    public bool DispatchOnSchedule { get; set; }

    public void BeginDispatch(CancellationToken cancellationToken) { }

    public static NoopBatchDispatcher Default { get; } = new();

    public void Schedule(Func<ValueTask> dispatch)
    {
        throw new NotImplementedException();
    }
}
