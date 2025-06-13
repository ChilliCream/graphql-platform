using System.Runtime.CompilerServices;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Fetching;
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution.Processing;

internal sealed partial class WorkScheduler(OperationContext operationContext)
{
    private readonly object _sync = new();
    private readonly WorkQueue _work = new();
    private readonly WorkQueue _serial = new();
    private readonly ProcessingPause _pause = new();

    private RequestContext _requestContext = null!;
    private IBatchDispatcher _batchDispatcher = null!;
    private IErrorHandler _errorHandler = null!;
    private ResultBuilder _result = null!;
    private IExecutionDiagnosticEvents _diagnosticEvents = null!;
    private CancellationToken _ct;

    private bool _hasBatches;
    private bool _isCompleted;
    private bool _isInitialized;

    public void Initialize(IBatchDispatcher batchDispatcher)
    {
        _batchDispatcher = batchDispatcher;
        _batchDispatcher.TaskEnqueued += BatchDispatcherEventHandler;

        _errorHandler = operationContext.ErrorHandler;
        _result = operationContext.Result;
        _diagnosticEvents = operationContext.DiagnosticEvents;
        _ct = operationContext.RequestAborted;

        _hasBatches = false;
        _isCompleted = false;
        _isInitialized = true;
    }

    public void Reset()
    {
        var batchDispatcher = _batchDispatcher;
        Clear();
        Initialize(batchDispatcher);
    }

    public void Clear()
    {
        _work.Clear();
        _serial.Clear();
        _pause.Reset();

        _batchDispatcher.TaskEnqueued -= BatchDispatcherEventHandler;
        _batchDispatcher = null!;

        _requestContext = null!;
        _errorHandler = null!;
        _result = null!;
        _diagnosticEvents = null!;
        _ct = CancellationToken.None;

        _hasBatches = false;
        _isCompleted = false;
        _isInitialized = false;
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
