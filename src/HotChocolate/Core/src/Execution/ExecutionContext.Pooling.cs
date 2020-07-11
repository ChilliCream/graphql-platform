using System.Threading;
using Microsoft.Extensions.ObjectPool;
using HotChocolate.Execution.Utilities;
using HotChocolate.Fetching;

namespace HotChocolate.Execution
{
    internal partial class ExecutionContext
        : IExecutionContext
    {
        private readonly TaskBacklog _taskBacklog;
        private readonly TaskStatistics _taskStatistics;
        private CancellationTokenSource _completed = default!;

        public ExecutionContext(ObjectPool<ResolverTask> resolverTaskPool)
        {
            _taskStatistics = new TaskStatistics();
            _taskBacklog = new TaskBacklog(_taskStatistics, resolverTaskPool);
            TaskPool = resolverTaskPool;
            TaskStats.StateChanged += TaskStatisticsEventHandler;
            TaskStats.AllTasksCompleted += OnCompleted;
        }

        public void Initialize(IBatchDispatcher batchDispatcher, CancellationToken requestAborted)
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
                catch { }
            }
        }

        public void Reset()
        {
            BatchDispatcher.TaskEnqueued -= BatchDispatcherEventHandler;
            BatchDispatcher = default!;
            _taskBacklog.Reset();
            _taskStatistics.Reset();

            TryComplete();
            _completed.Dispose();
            _completed = default!;
        }
    }
}
