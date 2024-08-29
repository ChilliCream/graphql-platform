using Microsoft.Extensions.ObjectPool;
using static HotChocolate.Execution.Processing.ResultPoolDefaults;

namespace HotChocolate.Execution.Processing;

internal sealed class ListResultPool(int maximumRetained, int maxAllowedCapacity, int bucketSize)
    : DefaultObjectPool<ResultBucket<ListResult>>(new BufferPolicy(maxAllowedCapacity, bucketSize), maximumRetained)
{
    private sealed class BufferPolicy(int maxAllowedCapacity, int bucketSize)
        : PooledObjectPolicy<ResultBucket<ListResult>>
    {
        private readonly ObjectPolicy _objectPolicy = new(maxAllowedCapacity);

        public override ResultBucket<ListResult> Create()
            => new(bucketSize, _objectPolicy);

        public override bool Return(ResultBucket<ListResult> obj)
        {
            obj.Reset();
            return true;
        }
    }

    private sealed class ObjectPolicy(int maxAllowedCapacity)
        : PooledObjectPolicy<ListResult>
    {
        public override ListResult Create() => new();

        public override bool Return(ListResult obj)
        {
            if (obj.Count > maxAllowedCapacity)
            {
                obj.Reset();
                return false;
            }

            obj.Reset();
            return true;
        }
    }
}
