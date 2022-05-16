using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing.Pooling;

internal sealed class ResultPool
{
    private readonly ObjectPool<ResultBuffer<ObjectResult>> _objectResultPool;
    private readonly ObjectPool<ResultBuffer<ObjectListResult>> _objectListResultPool;
    private readonly ObjectPool<ResultBuffer<ListResult>> _listResultPool;

    public ResultPool(
        ObjectPool<ResultBuffer<ObjectResult>> objectResultPool,
        ObjectPool<ResultBuffer<ObjectListResult>> objectListResultPool,
        ObjectPool<ResultBuffer<ListResult>> listResultPool)
    {
        _objectResultPool = objectResultPool;
        _objectListResultPool = objectListResultPool;
        _listResultPool = listResultPool;
    }

    public ResultBuffer<ObjectResult> GetObjectResult()
        => _objectResultPool.Get();

    public ResultBuffer<ObjectListResult> GetObjectResultList()
        => _objectListResultPool.Get();

    public ResultBuffer<ListResult> GetListResult()
        => _listResultPool.Get();

    public void Return(IList<ResultBuffer<ObjectResult>> buffers)
    {
        for (var i = 0; i < buffers.Count; i++)
        {
            _objectResultPool.Return(buffers[i]);
        }
    }

    public void Return(IList<ResultBuffer<ObjectListResult>> buffers)
    {
        for (var i = 0; i < buffers.Count; i++)
        {
            _objectListResultPool.Return(buffers[i]);
        }
    }

    public void Return(IList<ResultBuffer<ListResult>> buffers)
    {
        for (var i = 0; i < buffers.Count; i++)
        {
            _listResultPool.Return(buffers[i]);
        }
    }
}
