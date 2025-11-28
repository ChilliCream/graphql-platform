using HotChocolate.Execution.Instrumentation;

namespace HotChocolate.Execution.Processing;

internal sealed partial class ResultBuilder
{
    private RequestContext _context = null!;
    private IExecutionDiagnosticEvents _diagnosticEvents = null!;

    public ResultBuilder(ResultPool resultPool)
    {
        _resultPool = resultPool;
        InitializeResult();
    }

    public void Initialize(
        RequestContext context,
        IExecutionDiagnosticEvents diagnosticEvents)
    {
        _context = context;
        _diagnosticEvents = diagnosticEvents;
    }

    public void Clear()
    {
        _errors.Clear();
        _errorPaths.Clear();
        _fieldErrors.Clear();
        _nonNullViolations.Clear();
        _extensions.Clear();
        _contextData.Clear();
        _cleanupTasks.Clear();
        _removedResults.Clear();
        _patchIds.Clear();

        InitializeResult();

        _context = null!;
        _diagnosticEvents = null!;
        _data = null;
        _items = null;
        _path = null;
        _label = null;
        _hasNext = null;
        _requestIndex = null;
        _variableIndex = null;
        _singleErrorPerPath = false;
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
