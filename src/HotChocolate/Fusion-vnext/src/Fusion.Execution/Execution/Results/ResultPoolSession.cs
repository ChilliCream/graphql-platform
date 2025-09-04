using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution.Results;

public sealed class ResultPoolSession
{
    private readonly ResultDataPoolSession<ObjectResult> _objectResultPool;
    private readonly ResultDataPoolSession<LeafFieldResult> _leafFieldResultPool;
    private readonly ResultDataPoolSession<ListFieldResult> _listFieldResultPool;
    private readonly ResultDataPoolSession<ObjectFieldResult> _objectFieldResultPool;
    private readonly ResultDataPoolSession<ObjectListResult> _objectListResultPool;
    private readonly ResultDataPoolSession<NestedListResult> _nestedListResultPool;
    private readonly ResultDataPoolSession<LeafListResult> _leafListResultPool;
    private int _totalRents;

    internal ResultPoolSession(
        ObjectPool<ResultDataBatch<ObjectResult>> objectResultPool,
        ObjectPool<ResultDataBatch<LeafFieldResult>> leafFieldResultPool,
        ObjectPool<ResultDataBatch<ListFieldResult>> listFieldResultPool,
        ObjectPool<ResultDataBatch<ObjectFieldResult>> objectFieldResultPool,
        ObjectPool<ResultDataBatch<ObjectListResult>> objectListResultPool,
        ObjectPool<ResultDataBatch<NestedListResult>> nestedListResultPool,
        ObjectPool<ResultDataBatch<LeafListResult>> leafListResultPool)
    {
        _objectResultPool = new ResultDataPoolSession<ObjectResult>(objectResultPool);
        _leafFieldResultPool = new ResultDataPoolSession<LeafFieldResult>(leafFieldResultPool);
        _listFieldResultPool = new ResultDataPoolSession<ListFieldResult>(listFieldResultPool);
        _objectFieldResultPool = new ResultDataPoolSession<ObjectFieldResult>(objectFieldResultPool);
        _objectListResultPool = new ResultDataPoolSession<ObjectListResult>(objectListResultPool);
        _nestedListResultPool = new ResultDataPoolSession<NestedListResult>(nestedListResultPool);
        _leafListResultPool = new ResultDataPoolSession<LeafListResult>(leafListResultPool);

        ResultPoolEventSource.Log.SessionCreated();
    }

    public ObjectResult RentObjectResult()
    {
        Interlocked.Increment(ref _totalRents);
        return _objectResultPool.Rent();
    }

    public LeafFieldResult RentLeafFieldResult()
    {
        Interlocked.Increment(ref _totalRents);
        return _leafFieldResultPool.Rent();
    }

    public ListFieldResult RentListFieldResult()
    {
        Interlocked.Increment(ref _totalRents);
        return _listFieldResultPool.Rent();
    }

    public ObjectFieldResult RentObjectFieldResult()
    {
        Interlocked.Increment(ref _totalRents);
        return _objectFieldResultPool.Rent();
    }

    public ObjectListResult RentObjectListResult()
    {
        Interlocked.Increment(ref _totalRents);
        return _objectListResultPool.Rent();
    }

    public NestedListResult RentNestedListResult()
    {
        Interlocked.Increment(ref _totalRents);
        return _nestedListResultPool.Rent();
    }

    public LeafListResult RentLeafListResult()
    {
        Interlocked.Increment(ref _totalRents);
        return _leafListResultPool.Rent();
    }

    public void Reset()
    {
        var totalUsedBatches = 0;

        unchecked
        {
            totalUsedBatches =
                _objectResultPool.UsedBatchCount
                    + _leafFieldResultPool.UsedBatchCount
                    + _listFieldResultPool.UsedBatchCount
                    + _objectFieldResultPool.UsedBatchCount
                    + _objectListResultPool.UsedBatchCount
                    + _nestedListResultPool.UsedBatchCount
                    + _leafListResultPool.UsedBatchCount;
        }

        _objectResultPool.Reset();
        _leafFieldResultPool.Reset();
        _listFieldResultPool.Reset();
        _objectFieldResultPool.Reset();
        _objectListResultPool.Reset();
        _nestedListResultPool.Reset();
        _leafListResultPool.Reset();

        ResultPoolEventSource.Log.SessionDisposed(_totalRents, totalUsedBatches);
    }
}
