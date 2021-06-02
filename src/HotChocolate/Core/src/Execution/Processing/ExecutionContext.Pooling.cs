using System.Threading;
using HotChocolate.Execution.Processing.Tasks;
using HotChocolate.Fetching;
using Microsoft.Extensions.ObjectPool;
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution.Processing
{
    internal partial class ExecutionContext
    {
        private readonly OperationContext _operationContext;
        private readonly WorkBacklog _workBacklog;
        private readonly DeferredWorkBacklog _deferredWorkBacklog;
        private readonly ObjectPool<ResolverTask> _resolverTasks;
        private readonly ObjectPool<PureResolverTask> _pureResolverTasks;
        private readonly ObjectPool<IExecutionTask?[]> _taskBuffers;

        private CancellationTokenSource _completed = default!;
        private IBatchDispatcher _batchDispatcher = default!;
        private bool _isInitialized;

        public ExecutionContext(
            OperationContext operationContext,
            ObjectPool<ResolverTask> resolverTasks,
            ObjectPool<PureResolverTask> pureResolverTasks,
            ObjectPool<IExecutionTask?[]> taskBuffers)
        {
            _operationContext = operationContext;
            _workBacklog = new WorkBacklog();
            _deferredWorkBacklog = new DeferredWorkBacklog();
            _resolverTasks = resolverTasks;
            _pureResolverTasks = pureResolverTasks;
            _taskBuffers = taskBuffers;
            _workBacklog.BacklogEmpty += BatchDispatcherEventHandler;
        }

        public void Initialize(
            IBatchDispatcher batchDispatcher,
            CancellationToken requestAborted)
        {
            _completed = new CancellationTokenSource();
            requestAborted.Register(TryComplete);

            _batchDispatcher = batchDispatcher;
            _batchDispatcher.TaskEnqueued += BatchDispatcherEventHandler;
            _isInitialized = true;

            _workBacklog.Initialize(_operationContext, _operationContext.QueryPlan);
        }

        public void Clean()
        {
            if (_batchDispatcher is not null!)
            {
                _batchDispatcher.TaskEnqueued -= BatchDispatcherEventHandler;
                _batchDispatcher = default!;
            }

            _workBacklog.Clear();
            _deferredWorkBacklog.Clear();

            if (_completed is not null!)
            {
                TryComplete();
                _completed.Dispose();
                _completed = default!;
            }

            _isInitialized = false;
        }

        public void Reset()
        {
            ResetStateMachine();

            if (_completed is not null!)
            {
                TryComplete();
                _completed.Dispose();
                _completed = new CancellationTokenSource();
            }
        }

        public void ResetStateMachine()
        {
            _workBacklog.Clear();
            _workBacklog.Initialize(_operationContext, _operationContext.QueryPlan);
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

        private void AssertNotPooled()
        {
            if (!_isInitialized)
            {
                throw Object_Not_Initialized();
            }
        }
    }
}
