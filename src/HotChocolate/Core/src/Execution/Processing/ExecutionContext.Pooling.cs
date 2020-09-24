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
        private bool _reset = true;

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
            if (!_reset)
            {
                Reset();
            }

            _completed = new CancellationTokenSource();
            requestAborted.Register(TryComplete);

            BatchDispatcher = batchDispatcher;
            BatchDispatcher.TaskEnqueued += BatchDispatcherEventHandler;
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

        public void Reset()
        {
            if (!_reset)
            {
                BatchDispatcher.TaskEnqueued -= BatchDispatcherEventHandler;
                BatchDispatcher = default!;
                _taskBacklog.Reset();
                _deferredTaskBacklog.Reset();
                _taskStatistics.Reset();

                TryComplete();
                _completed.Dispose();
                _completed = default!;
                _reset = true;
            }
        }

        void IExecutionContext.Reset()
        {
            _taskBacklog.Reset();
            _taskStatistics.Reset();

            TryComplete();
            _completed.Dispose();
            _completed = new CancellationTokenSource();
        }
    }
}
