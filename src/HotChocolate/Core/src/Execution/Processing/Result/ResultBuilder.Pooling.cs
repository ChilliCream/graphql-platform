using HotChocolate.Execution.Instrumentation;

namespace HotChocolate.Execution.Processing;

internal sealed partial class ResultBuilder
{
    private IRequestContext _context = default!;
    private IExecutionDiagnosticEvents _diagnosticEvents = default!;

    public ResultBuilder(ResultPool resultPool)
    {
        _resultPool = resultPool;
        InitializeResult();
    }

    public void Initialize(
        IRequestContext context,
        IExecutionDiagnosticEvents diagnosticEvents)
    {
        _context = context;
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

        _context = default!;
        _diagnosticEvents = default!;
        _data = null;
        _items = null;
        _path = null;
        _label = null;
        _hasNext = null;
        _requestIndex = null;
        _variableIndex = null;
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
