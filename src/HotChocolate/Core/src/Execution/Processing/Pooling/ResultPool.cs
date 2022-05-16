using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing.Pooling;

internal sealed class ResultPool
{
    private readonly ObjectResultPool _objectResultPool;
    private readonly ObjectListResultPool _objectListResultPool;
    private readonly ListResultPool _listResultPool;

    public ResultPool(
        ObjectResultPool objectResultPool,
        ObjectListResultPool objectListResultPool,
        ListResultPool listResultPool)
    {
        _objectResultPool = objectResultPool;
        _objectListResultPool = objectListResultPool;
        _listResultPool = listResultPool;
    }

    public ResultBucket<ObjectResult> GetObjectBucket()
        => _objectResultPool.Get();

    public ResultBucket<ObjectListResult> GetObjectListBucket()
        => _objectListResultPool.Get();

    public ResultBucket<ListResult> GetListBucket()
        => _listResultPool.Get();

    public void Return(IList<ResultBucket<ObjectResult>> buffers)
    {
        for (var i = 0; i < buffers.Count; i++)
        {
            _objectResultPool.Return(buffers[i]);
        }
    }

    public void Return(IList<ResultBucket<ObjectListResult>> buffers)
    {
        for (var i = 0; i < buffers.Count; i++)
        {
            _objectListResultPool.Return(buffers[i]);
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
