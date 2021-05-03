using System;
using System.Threading;
using HotChocolate.Execution.Processing.Tasks;
using HotChocolate.Fetching;
using Microsoft.Extensions.ObjectPool;
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution.Processing
{
    internal partial class ExecutionContext
    {
        private readonly IExecutionTaskContext _taskContext;
        private readonly TaskBacklog _taskBacklog;
        private readonly TaskStatistics _taskStatistics;
        private readonly IDeferredTaskBacklog _deferredTaskBacklog;
        private readonly ObjectPool<ResolverTask> _taskPool;
        private readonly ObjectPool<PureResolverTask> _pureTaskPool;
        private readonly ObjectPool<BatchExecutionTask> _batchTaskPool;
        private readonly RequestTaskScheduler _taskScheduler = new();
        private CancellationTokenSource _completed = default!;
        private IBatchDispatcher _batchDispatcher = default!;

        private bool _isPooled = true;

        public ExecutionContext(
            IExecutionTaskContext taskContext,
            ObjectPool<ResolverTask> resolverTaskPool,
            ObjectPool<PureResolverTask> pureResolverTaskPool,
            ObjectPool<BatchExecutionTask> batchTaskPool)
        {
            _taskContext = taskContext;
            _taskStatistics = new TaskStatistics();
            _taskBacklog = new TaskBacklog();
            _deferredTaskBacklog = new DeferredTaskBacklog();
            _taskPool = resolverTaskPool;
            _pureTaskPool = pureResolverTaskPool;
            _batchTaskPool = batchTaskPool;
            _taskStatistics.StateChanged += TaskStatisticsEventHandler;
            _taskStatistics.AllTasksCompleted += OnCompleted;
            _taskBacklog.BacklogEmpty += (_, _) => BeginTryDispatchBatches();
        }

        public void Initialize(
            IBatchDispatcher batchDispatcher,
            CancellationToken requestAborted)
        {
            _taskStatistics.Clear();

            _completed = new CancellationTokenSource();
            requestAborted.Register(TryComplete);

            _batchDispatcher = batchDispatcher;
            _batchDispatcher.TaskEnqueued += BatchDispatcherEventHandler;

            _isPooled = false;
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
            if (_batchDispatcher is not null!)
            {
                _batchDispatcher.TaskEnqueued -= BatchDispatcherEventHandler;
                _batchDispatcher = default!;
            }

            _taskBacklog.Clear();
            _deferredTaskBacklog.Clear();
            _taskStatistics.Clear();

            if (_completed is not null!)
            {
                TryComplete();
                _completed.Dispose();
                _completed = default!;
            }

            _isPooled = true;
        }

        public void Reset()
        {
            _taskBacklog.Clear();
            _taskStatistics.Clear();

            if (_completed is not null!)
            {
                TryComplete();
                _completed.Dispose();
                _completed = new CancellationTokenSource();
            }
        }

        private void AssertNotPooled()
        {
            if (_isPooled)
            {
                throw Object_Returned_To_Pool();
            }
        }
    }
}
