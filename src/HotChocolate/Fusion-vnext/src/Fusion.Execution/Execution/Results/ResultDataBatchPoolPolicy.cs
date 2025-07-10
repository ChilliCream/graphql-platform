using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution;

internal sealed class ResultDataBatchPoolPolicy<T>
    : PooledObjectPolicy<ResultDataBatch<T>> where T : ResultData, new()
{
    private readonly int _batchSize;
    private readonly int _defaultCapacity;
    private readonly int _maxAllowedCapacity;

    public ResultDataBatchPoolPolicy(int batchSize, int defaultCapacity = 8, int maxAllowedCapacity = 16)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(batchSize, 8);
        ArgumentOutOfRangeException.ThrowIfLessThan(defaultCapacity, 8);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxAllowedCapacity, 16);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(defaultCapacity, maxAllowedCapacity);

        _batchSize = batchSize;
        _defaultCapacity = defaultCapacity;
        _maxAllowedCapacity = maxAllowedCapacity;
    }

    public override ResultDataBatch<T> Create()
        => new(_batchSize, _defaultCapacity, _maxAllowedCapacity);

    public override bool Return(ResultDataBatch<T> obj)
    {
        obj.Reset();
        return true;
    }
}
