using Microsoft.Extensions.ObjectPool;
using static HotChocolate.Execution.Processing.ResultPoolDefaults;

namespace HotChocolate.Execution.Processing;

internal sealed class ObjectResultPool : DefaultObjectPool<ResultBucket<ObjectResult>>
{
    public ObjectResultPool(int maximumRetained, int maxAllowedCapacity)
        : base(new BufferPolicy(maxAllowedCapacity), maximumRetained)
    {
    }

    private sealed class BufferPolicy : PooledObjectPolicy<ResultBucket<ObjectResult>>
    {
        private readonly ObjectPolicy _objectPolicy;

        public BufferPolicy(int maxAllowedCapacity)
        {
            _objectPolicy = new ObjectPolicy(maxAllowedCapacity);
        }

        public override ResultBucket<ObjectResult> Create()
            => new(BucketSize, _objectPolicy);

        public override bool Return(ResultBucket<ObjectResult> obj)
        {
            obj.Reset();
            return true;
        }
    }

    private sealed class ObjectPolicy : PooledObjectPolicy<ObjectResult>
    {
        private readonly int _maxAllowedCapacity;

        public ObjectPolicy(int maxAllowedCapacity)
        {
            _maxAllowedCapacity = maxAllowedCapacity;
        }

        public override ObjectResult Create() => new();

        public override bool Return(ObjectResult obj)
        {
            if (obj.Capacity > _maxAllowedCapacity)
            {
                obj.Reset();
                return false;
            }

            obj.Reset();
            return true;
        }
    }
}
