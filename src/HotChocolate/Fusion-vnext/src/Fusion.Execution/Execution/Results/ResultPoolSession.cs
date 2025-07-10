using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution;

public sealed class ResultPoolSession
{
    private readonly ResultDataPoolSession<ObjectResult> _objectResultPool;
    private readonly ResultDataPoolSession<LeafFieldResult> _leafFieldResultPool;
    private readonly ResultDataPoolSession<ListFieldResult> _listFieldResultPool;
    private readonly ResultDataPoolSession<ObjectFieldResult> _objectFieldResultPool;
    private readonly ResultDataPoolSession<ObjectListResult> _objectListResultPool;
    private readonly ResultDataPoolSession<NestedListResult> _nestedListResultPool;
    private readonly ResultDataPoolSession<LeafListResult> _leafListResultPool;

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
    }

    public ObjectResult RentObjectResult() => _objectResultPool.Rent();

    public LeafFieldResult RentLeafFieldResult() => _leafFieldResultPool.Rent();

    public ListFieldResult RentListFieldResult() => _listFieldResultPool.Rent();

    public ObjectFieldResult RentObjectFieldResult() => _objectFieldResultPool.Rent();

    public ObjectListResult RentObjectListResult() => _objectListResultPool.Rent();

    public NestedListResult RentNestedListResult() => _nestedListResultPool.Rent();

    public LeafListResult RentLeafListResult() => _leafListResultPool.Rent();

    public void Reset()
    {
        _objectResultPool.Reset();
        _leafFieldResultPool.Reset();
        _listFieldResultPool.Reset();
        _objectFieldResultPool.Reset();
        _objectListResultPool.Reset();
        _nestedListResultPool.Reset();
        _leafListResultPool.Reset();
    }
}
