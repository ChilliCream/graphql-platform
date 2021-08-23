using System.Threading;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Execution.Processing.Internal;
using HotChocolate.Execution.Processing.Plan;
using HotChocolate.Fetching;
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution.Processing
{
    internal partial class WorkScheduler
    {
        private readonly object _sync = new();
        private readonly WorkQueue _work = new();
        private readonly WorkQueue _serial = new();
        private readonly SuspendedWorkQueue _suspended = new();
        private readonly QueryPlanStateMachine _stateMachine = new();

        private readonly OperationContext _operationContext;
        private readonly DeferredWorkBacklog _deferredWorkBacklog = new();
        private readonly Pause _pause;

        private bool _processing;
        private bool _completed;

        private IRequestContext _requestContext = default!;
        private IBatchDispatcher _batchDispatcher = default!;
        private IErrorHandler _errorHandler = default!;
        private IResultHelper _result = default!;
        private IDiagnosticEvents _diagnosticEvents = default!;
        private CancellationToken _requestAborted;

        private bool _isInitialized;

        public WorkScheduler(OperationContext operationContext)
        {
            _operationContext = operationContext;
            _pause = new(_sync);
        }

        public void Initialize(IBatchDispatcher batchDispatcher)
        {
            Clear();

            _requestContext = _operationContext.RequestContext;
            _errorHandler = _operationContext.ErrorHandler;
            _result = _operationContext.Result;
            _diagnosticEvents = _operationContext.DiagnosticEvents;
            _requestAborted = _operationContext.RequestAborted;
            _batchDispatcher = batchDispatcher;

            _stateMachine.Initialize(_operationContext, _operationContext.QueryPlan);
            _requestContext.RequestAborted.Register(Cancel);

            _batchDispatcher.TaskEnqueued += BatchDispatcherEventHandler;

            _isInitialized = true;
        }

        public void Clear()
        {
            lock (_sync)
            {
                _pause.TryContinueUnsafe();

                if (_batchDispatcher is not null!)
                {
                    _batchDispatcher.TaskEnqueued -= BatchDispatcherEventHandler;
                    _batchDispatcher = default!;
                }

                _work.Clear();
                _suspended.Clear();
                _stateMachine.Clear();
                _deferredWorkBacklog.Clear();
                _processing = false;
                _completed = false;

                _requestContext = default!;
                _errorHandler = default!;
                _result = default!;
                _diagnosticEvents = default!;
                _requestAborted = default;

                _isInitialized = false;
            }
        }

        public void Reset()
        {
            ResetStateMachine();
        }

        public void ResetStateMachine()
        {
            lock (_sync)
            {
                _pause.TryContinueUnsafe();

                _work.Clear();
                _suspended.Clear();
                _stateMachine.Clear();
                _stateMachine.Initialize(_operationContext, _operationContext.QueryPlan);

                _processing = false;
                _completed = false;
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
