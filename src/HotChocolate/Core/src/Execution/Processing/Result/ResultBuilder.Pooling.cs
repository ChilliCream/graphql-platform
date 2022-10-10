using HotChocolate.Execution.Instrumentation;

namespace HotChocolate.Execution.Processing;

internal sealed partial class ResultBuilder
{
    private IOperation _operation = default!;
    private IErrorHandler _errorHandler = default!;
    private IExecutionDiagnosticEvents _diagnosticEvents = default!;

    public ResultBuilder(ResultPool resultPool)
    {
        _resultPool = resultPool;
        InitializeResult();
    }

    public void Initialize(
        IOperation operation,
        IErrorHandler errorHandler,
        IExecutionDiagnosticEvents diagnosticEvents)
    {
        _operation = operation;
        _errorHandler = errorHandler;
        _diagnosticEvents = diagnosticEvents;
    }

    public void Clear()
    {
        _errors.Clear();
        _fieldErrors.Clear();
        _nonNullViolations.Clear();
        _extensions.Clear();
        _contextData.Clear();
        _cleanupTasks.Clear();
        _removedResults.Clear();
        _patchIds.Clear();

        InitializeResult();

        _operation = default!;
        _errorHandler = default!;
        _diagnosticEvents = default!;
        _data = null;
        _items = null;
        _path = null;
        _label = null;
        _hasNext = null;
    }

    private void InitializeResult()
    {
        _resultOwner = new ResultMemoryOwner(_resultPool);
        _objectBucket = _resultPool.GetObjectBucket();
        _resultOwner.ObjectBuckets.Add(_objectBucket);
        _listBucket = _resultPool.GetListBucket();
        _resultOwner.ListBuckets.Add(_listBucket);
    }
}
