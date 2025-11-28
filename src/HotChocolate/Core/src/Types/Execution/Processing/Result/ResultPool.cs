namespace HotChocolate.Execution.Processing;

internal sealed class ResultPool
{
    private readonly ObjectResultPool _objectResultPool;
    private readonly ListResultPool _listResultPool;

    public ResultPool(
        ObjectResultPool objectResultPool,
        ListResultPool listResultPool)
    {
        _objectResultPool = objectResultPool;
        _listResultPool = listResultPool;
    }

    public ResultBucket<ObjectResult> GetObjectBucket()
        => _objectResultPool.Get();

    public ResultBucket<ListResult> GetListBucket()
        => _listResultPool.Get();

    public void Return(IList<ResultBucket<ObjectResult>> buffers)
    {
        for (var i = 0; i < buffers.Count; i++)
        {
            _objectResultPool.Return(buffers[i]);
        }
    }

    public void Return(IList<ResultBucket<ListResult>> buffers)
    {
        for (var i = 0; i < buffers.Count; i++)
        {
            _listResultPool.Return(buffers[i]);
        }
    }
}
