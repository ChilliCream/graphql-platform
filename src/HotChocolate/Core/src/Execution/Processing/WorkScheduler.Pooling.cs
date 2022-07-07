using System.Runtime.CompilerServices;
using System.Threading;
using HotChocolate.Execution.Instrumentation;
using HotChocolate.Fetching;
using static HotChocolate.Execution.ThrowHelper;

namespace HotChocolate.Execution.Processing;

internal sealed partial class WorkScheduler
{
    private readonly object _sync = new();
    private readonly WorkQueue _work = new();
    private readonly WorkQueue _serial = new();
    private readonly ProcessingPause _pause = new();
    private readonly OperationContext _operationContext;

    private IRequestContext _requestContext = default!;
    private IBatchDispatcher _batchDispatcher = default!;
    private IErrorHandler _errorHandler = default!;
    private ResultBuilder _result = default!;
    private IExecutionDiagnosticEvents _diagnosticEvents = default!;
    private CancellationToken _ct;

    private bool _hasBatches;
    private bool _isCompleted;
    private bool _isInitialized;

    public WorkScheduler(OperationContext operationContext)
    {
        _operationContext = operationContext;
    }

    public void Initialize(IBatchDispatcher batchDispatcher)
    {
        _batchDispatcher = batchDispatcher;
        _batchDispatcher.TaskEnqueued += BatchDispatcherEventHandler;

        _errorHandler = _operationContext.ErrorHandler;
        _result = _operationContext.Result;
        _diagnosticEvents = _operationContext.DiagnosticEvents;
        _ct = _operationContext.RequestAborted;

        _hasBatches = false;
        _isCompleted = false;
        _isInitialized = true;
    }

    public void Clear()
    {
        _work.Clear();
        _serial.Clear();
        _pause.Reset();

        _batchDispatcher.TaskEnqueued -= BatchDispatcherEventHandler;
        _batchDispatcher = default!;

        _requestContext = default!;
        _errorHandler = default!;
        _result = default!;
        _diagnosticEvents = default!;
        _ct = default;

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
