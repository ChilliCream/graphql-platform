using System.Runtime.CompilerServices;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Fetching;
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution.Processing;

internal sealed partial class WorkScheduler(OperationContext operationContext)
{
#if NET9_0_OR_GREATER
    private readonly Lock _sync = new();
#else
    private readonly object _sync = new();
#endif
    private readonly WorkQueue _work = new();
    private readonly WorkQueue _serial = new();
    private readonly AsyncManualResetEvent _signal = new();

    private RequestContext _requestContext = null!;
    private IBatchDispatcher _batchDispatcher = null!;
    private IDisposable _batchDispatcherSession = null!;
    private OperationResultBuilder _result = null!;
    private IErrorHandler _errorHandler = null!;
    private IExecutionDiagnosticEvents _diagnosticEvents = null!;
    private readonly HashSet<uint> _completed = [];
    private uint _nextId = 1;
    private CancellationToken _ct;

    private int _hasBatches;
    private bool _isCompleted;
    private bool _isInitialized;
    private bool _executionCompleted;
    private bool _clearAfterExecution;

    public void Initialize(RequestContext requestContext, IBatchDispatcher batchDispatcher)
    {
        _requestContext = requestContext;
        _batchDispatcher = batchDispatcher;
        _batchDispatcherSession = _batchDispatcher.Subscribe(this);

        _result = operationContext.Result;
        _errorHandler = operationContext.ErrorHandler;
        _diagnosticEvents = operationContext.DiagnosticEvents;
        _ct = operationContext.RequestAborted;

        _hasBatches = 0;
        _isCompleted = false;
        _executionCompleted = false;
        _clearAfterExecution = false;
        _isInitialized = true;
    }

    public void Clear()
    {
        _work.Clear();
        _serial.Clear();
        _completed.Clear();
        _activeBranches.Clear();
        _activePaths.Clear();
        _pendingBatches.Clear();
        _signal.Reset();
        ReleaseExecutionGroupScopes();

        _result = null!;
        _batchDispatcherSession?.Dispose();
        _batchDispatcherSession = null!;
        _batchDispatcher = null!;

        _nextId = 1;
        _requestContext = null!;
        _errorHandler = null!;
        _diagnosticEvents = null!;
        _ct = CancellationToken.None;

        _hasBatches = 0;
        _isCompleted = false;
        _executionCompleted = false;
        _clearAfterExecution = false;
        _isInitialized = false;
    }

    public void ClearAfterExecution()
    {
        bool clear;

        lock (_sync)
        {
            clear = _executionCompleted;

            if (!clear)
            {
                _clearAfterExecution = true;
            }
        }

        if (clear)
        {
            operationContext.Clean();
        }
    }

    public void CompleteExecution()
    {
        bool clear;

        lock (_sync)
        {
            _executionCompleted = true;
            clear = _clearAfterExecution;
        }

        if (clear)
        {
            operationContext.Clean();
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AssertNotPooled()
    {
        if (!_isInitialized)
        {
            throw Object_Not_Initialized();
        }
    }
}
