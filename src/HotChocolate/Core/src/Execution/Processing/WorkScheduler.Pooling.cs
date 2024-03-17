using System.Runtime.CompilerServices;
using System.Threading;
using GreenDonut.DependencyInjection;
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
    private readonly DefaultBatchScheduler _batchScheduler = new();

    private IRequestContext _requestContext = default!;
    private IDataLoaderContext _dataLoaderContext = default!;
    private IErrorHandler _errorHandler = default!;
    private ResultBuilder _result = default!;
    private IExecutionDiagnosticEvents _diagnosticEvents = default!;
    private SynchronizedAutoScheduler? _synchronizedAutoScheduler;
    private CancellationToken _ct;

    private bool _hasBatches;
    private bool _isCompleted;
    private bool _isInitialized;

    public void Initialize()
    {
        _batchScheduler.RegisterTaskEnqueuedCallback(BatchDispatcherEventHandler);

        _dataLoaderContext = operationContext.DataLoaderContext;
        _errorHandler = operationContext.ErrorHandler;
        _result = operationContext.Result;
        _diagnosticEvents = operationContext.DiagnosticEvents;
        _ct = operationContext.RequestAborted;

        _hasBatches = false;
        _isCompleted = false;
        _isInitialized = true;
        
        UseBatchScheduler();
    }

    public void Clear()
    {
        _work.Clear();
        _serial.Clear();
        _pause.Reset();

        _batchScheduler.Dispose();
        _synchronizedAutoScheduler?.Dispose();
        _synchronizedAutoScheduler = null;

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
