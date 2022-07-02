namespace HotChocolate.Execution.Processing;

internal sealed partial class ResultBuilder
{

    public ResultBuilder(ResultPool resultPool)
    {
        _resultPool = resultPool;
        InitializeResult();
    }

    public void Clear()
    {
        _errors.Clear();
        _fieldErrors.Clear();
        _nonNullViolations.Clear();
        _extensions.Clear();
        _contextData.Clear();

        InitializeResult();

        _data = null;
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
