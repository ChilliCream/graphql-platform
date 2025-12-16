using System.Collections.Concurrent;
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
    private readonly AsyncManualResetEvent _signal = new();

    private RequestContext _requestContext = null!;
    private IBatchDispatcher _batchDispatcher = null!;
    private IDisposable _batchDispatcherSession = null!;
    private OperationResultBuilder _result = null!;
    private IErrorHandler _errorHandler = null!;
    private IExecutionDiagnosticEvents _diagnosticEvents = null!;
    private readonly ConcurrentDictionary<uint, bool> _completed = new();
    private uint _nextId = 1;
    private CancellationToken _ct;

    private int _hasBatches;
    private bool _isCompleted;
    private bool _isInitialized;

    public void Initialize(IBatchDispatcher batchDispatcher)
    {
        _batchDispatcher = batchDispatcher;
        _batchDispatcherSession = _batchDispatcher.Subscribe(this);

        _result = operationContext.Result;
        _errorHandler = operationContext.ErrorHandler;
        _diagnosticEvents = operationContext.DiagnosticEvents;
        _ct = operationContext.RequestAborted;

        _hasBatches = 0;
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
        _completed.Clear();
        _signal.Reset();

        _result = null!;
        _batchDispatcherSession.Dispose();
        _batchDispatcherSession = null!;
        _batchDispatcher = null!;

        _nextId = 1;
        _requestContext = null!;
        _errorHandler = null!;
        _diagnosticEvents = null!;
        _ct = CancellationToken.None;

        _hasBatches = 0;
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
