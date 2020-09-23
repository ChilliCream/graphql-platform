using System.Threading;
using Microsoft.Extensions.ObjectPool;
using HotChocolate.Execution.Processing;
using HotChocolate.Fetching;

namespace HotChocolate.Execution
{
    internal partial class ExecutionContext
        : IExecutionContext
    {
        private readonly IExecutionTaskContext _taskContext;
        private readonly TaskBacklog _taskBacklog;
        private readonly DeferredTaskBacklog _deferredTaskBacklog;
        private readonly TaskStatistics _taskStatistics;
        private CancellationTokenSource _completed = default!;

        public ExecutionContext(
            IExecutionTaskContext taskContext,
            ObjectPool<ResolverTask> resolverTaskPool)
        {
            _taskContext = taskContext;
            _taskStatistics = new TaskStatistics();
            _taskBacklog = new TaskBacklog(_taskStatistics, resolverTaskPool);
            _deferredTaskBacklog = new DeferredTaskBacklog();

            TaskPool = resolverTaskPool;
            TaskStats.StateChanged += TaskStatisticsEventHandler;
            TaskStats.AllTasksCompleted += OnCompleted;
        }

        public void Initialize(
            IBatchDispatcher batchDispatcher,
            CancellationToken requestAborted)
        {
            _completed = new CancellationTokenSource();
            requestAborted.Register(TryComplete);

            BatchDispatcher = batchDispatcher;
            BatchDispatcher.TaskEnqueued += BatchDispatcherEventHandler;
        }

        private void TryComplete()
        {
            if (_completed is { })
            {
                try
                {
                    if (!_completed.IsCancellationRequested)
                    {
                        _completed.Cancel();
                    }
                }
                catch
                {
                    // ignore if we could not cancel the completed task source.
                }
            }
        }

        public void Reset()
        {
            BatchDispatcher.TaskEnqueued -= BatchDispatcherEventHandler;
            BatchDispatcher = default!;
            _taskBacklog.Reset();
            _deferredTaskBacklog.Reset();
            _taskStatistics.Reset();

            TryComplete();
            _completed.Dispose();
            _completed = default!;
        }
    }
}
