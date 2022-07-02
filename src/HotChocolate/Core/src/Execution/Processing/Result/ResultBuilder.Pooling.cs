namespace HotChocolate.Execution.Processing;

internal sealed partial class ResultBuilder
{
    public ResultBuilder(ResultPool resultPool)
    {
        _resultPool = resultPool;
        
        _resultOwner = new ResultMemoryOwner(resultPool);
        _objectBucket = _resultPool.GetObjectBucket();
        _listBucket = _resultPool.GetListBucket();
    }

    public void Clear()
    {
        _errors.Clear();
        _fieldErrors.Clear();
        _nonNullViolations.Clear();
        _extensions.Clear();
        _contextData.Clear();

        _resultOwner = new ResultMemoryOwner(_resultPool);
        _objectBucket = _resultPool.GetObjectBucket();
        _listBucket = _resultPool.GetListBucket();

        _data = null;
        _path = null;
        _label = null;
        _hasNext = null;
    }
}
