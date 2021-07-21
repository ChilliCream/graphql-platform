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

        private CancellationTokenSource _completed = default!;
        private IBatchDispatcher _batchDispatcher = default!;
        private bool _isInitialized;

        public ExecutionContext(
            OperationContext operationContext,
            ObjectPool<ResolverTask> resolverTasks)
        {
            _operationContext = operationContext;
            _workBacklog = new WorkBacklog();
            _deferredWorkBacklog = new DeferredWorkBacklog();
            _resolverTasks = resolverTasks;
        }

        public void Initialize(IBatchDispatcher batchDispatcher)
        {
            _completed = new CancellationTokenSource();

            _operationContext.RequestAborted.Register(TryComplete);

            _batchDispatcher = batchDispatcher;
            _isInitialized = true;

            _workBacklog.Initialize(_operationContext);
        }

        public void Clean()
        {
            _batchDispatcher = default!;

            if (_completed is not null!)
            {
                TryComplete();
                _completed.Dispose();
                _completed = default!;
            }

            _workBacklog.Clear();
            _deferredWorkBacklog.Clear();

            _isInitialized = false;
        }

        public void Reset()
        {
            if (_completed is not null!)
            {
                TryComplete();
                _completed.Dispose();
                _completed = new CancellationTokenSource();
            }

            ResetStateMachine();
        }

        public void ResetStateMachine() => _workBacklog.Initialize(_operationContext);

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
