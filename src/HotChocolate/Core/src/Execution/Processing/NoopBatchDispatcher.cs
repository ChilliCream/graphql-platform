using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Fetching;

namespace HotChocolate.Execution.Processing;

internal class NoopBatchDispatcher : IBatchDispatcher
{
    public event EventHandler? TaskEnqueued;

    public bool HasTasks => false;

    public bool DispatchOnSchedule { get; set; } = false;

    public void BeginDispatch(CancellationToken cancellationToken) { }

    public static NoopBatchDispatcher Default { get; } = new();
}
