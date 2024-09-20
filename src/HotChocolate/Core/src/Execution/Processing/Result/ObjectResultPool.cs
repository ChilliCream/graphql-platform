using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing;

internal sealed class ObjectResultPool(int maximumRetained, int maxAllowedCapacity, int bucketSize)
    : DefaultObjectPool<ResultBucket<ObjectResult>>(new BufferPolicy(maxAllowedCapacity, bucketSize), maximumRetained)
{
    private sealed class BufferPolicy(int maxAllowedCapacity, int bucketSize)
        : PooledObjectPolicy<ResultBucket<ObjectResult>>
    {
        private readonly ObjectPolicy _objectPolicy = new(maxAllowedCapacity);

        public override ResultBucket<ObjectResult> Create()
            => new(bucketSize, _objectPolicy);

        public override bool Return(ResultBucket<ObjectResult> obj)
        {
            obj.Reset();
            return true;
        }
    }

    private sealed class ObjectPolicy(int maxAllowedCapacity)
        : PooledObjectPolicy<ObjectResult>
    {
        public override ObjectResult Create() => new();

        public override bool Return(ObjectResult obj)
        {
            if (obj.Capacity > maxAllowedCapacity)
            {
                obj.Reset();
                return false;
            }

            obj.Reset();
            return true;
        }
    }
}
