using System.Threading;
using HotChocolate.Fetching;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing
{
    internal partial class ExecutionContext
    {
        private readonly IExecutionTaskContext _taskContext;
        private readonly TaskBacklog _taskBacklog;
        private readonly DeferredTaskBacklog _deferredTaskBacklog;
        private readonly TaskStatistics _taskStatistics;
        private CancellationTokenSource _completed = default!;
        private bool _clean = true;

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
            if (!_clean)
            {
                Clean();
            }

            _completed = new CancellationTokenSource();
            requestAborted.Register(TryComplete);

            BatchDispatcher = batchDispatcher;
            BatchDispatcher.TaskEnqueued += BatchDispatcherEventHandler;
            _clean = false;
        }

        private void TryComplete()
        {
            if (_completed is not null!)
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

        public void Clean()
        {
            if (!_clean)
            {
                BatchDispatcher.TaskEnqueued -= BatchDispatcherEventHandler;
                BatchDispatcher = default!;
                _taskBacklog.Clear();
                _deferredTaskBacklog.Clear();
                _taskStatistics.Clear();

                TryComplete();
                _completed.Dispose();
                _completed = default!;
                _clean = true;
            }
        }

        public void Reset()
        {
            _taskBacklog.Clear();
            _taskStatistics.Clear();

            TryComplete();
            _completed.Dispose();
            _completed = new CancellationTokenSource();
        }
    }
}
