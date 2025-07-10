using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution;

internal sealed class ResultPoolSessionPolicy
    : PooledObjectPolicy<ResultPoolSession>
{
    private readonly ObjectPool<ResultDataBatch<ObjectResult>> _objectResultPool;
    private readonly ObjectPool<ResultDataBatch<LeafFieldResult>> _leafFieldResultPool;
    private readonly ObjectPool<ResultDataBatch<ListFieldResult>> _listFieldResultPool;
    private readonly ObjectPool<ResultDataBatch<ObjectFieldResult>> _objectFieldResultPool;
    private readonly ObjectPool<ResultDataBatch<ObjectListResult>> _objectListResultPool;
    private readonly ObjectPool<ResultDataBatch<NestedListResult>> _nestedListResultPool;
    private readonly ObjectPool<ResultDataBatch<LeafListResult>> _leafListResultPool;

    public ResultPoolSessionPolicy(
        ObjectPool<ResultDataBatch<ObjectResult>> objectResultPool,
        ObjectPool<ResultDataBatch<LeafFieldResult>> leafFieldResultPool,
        ObjectPool<ResultDataBatch<ListFieldResult>> listFieldResultPool,
        ObjectPool<ResultDataBatch<ObjectFieldResult>> objectFieldResultPool,
        ObjectPool<ResultDataBatch<ObjectListResult>> objectListResultPool,
        ObjectPool<ResultDataBatch<NestedListResult>> nestedListResultPool,
        ObjectPool<ResultDataBatch<LeafListResult>> leafListResultPool)
    {
        ArgumentNullException.ThrowIfNull(objectResultPool);
        ArgumentNullException.ThrowIfNull(leafFieldResultPool);
        ArgumentNullException.ThrowIfNull(listFieldResultPool);
        ArgumentNullException.ThrowIfNull(objectFieldResultPool);
        ArgumentNullException.ThrowIfNull(objectListResultPool);
        ArgumentNullException.ThrowIfNull(nestedListResultPool);
        ArgumentNullException.ThrowIfNull(leafListResultPool);

        _objectResultPool = objectResultPool;
        _leafFieldResultPool = leafFieldResultPool;
        _listFieldResultPool = listFieldResultPool;
        _objectFieldResultPool = objectFieldResultPool;
        _objectListResultPool = objectListResultPool;
        _nestedListResultPool = nestedListResultPool;
        _leafListResultPool = leafListResultPool;
    }

    public override ResultPoolSession Create()
    {
        return new ResultPoolSession(
            _objectResultPool,
            _leafFieldResultPool,
            _listFieldResultPool,
            _objectFieldResultPool,
            _objectListResultPool,
            _nestedListResultPool,
            _leafListResultPool);
    }

    public override bool Return(ResultPoolSession obj)
    {
        obj.Reset();
        return true;
    }
}
