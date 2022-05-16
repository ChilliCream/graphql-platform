using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing.Pooling;

internal sealed class ObjectListResultPool : DefaultObjectPool<ResultBuffer<ObjectListResult>>
{
    public ObjectListResultPool(int maximumRetained, int maxAllowedCapacity = 256)
        : base(new BufferPolicy(maxAllowedCapacity), maximumRetained)
    {
    }

    private sealed class BufferPolicy : PooledObjectPolicy<ResultBuffer<ObjectListResult>>
    {
        private readonly ObjectPolicy _objectPolicy;

        public BufferPolicy(int maxAllowedCapacity)
        {
            _objectPolicy = new ObjectPolicy(maxAllowedCapacity);
        }

        public override ResultBuffer<ObjectListResult> Create()
            => new(16, _objectPolicy);

        public override bool Return(ResultBuffer<ObjectListResult> obj)
        {
            obj.Reset();
            return true;
        }
    }

    private sealed class ObjectPolicy : PooledObjectPolicy<ObjectListResult>
    {
        private readonly int _maxAllowedCapacity;

        public ObjectPolicy(int maxAllowedCapacity)
        {
            _maxAllowedCapacity = maxAllowedCapacity;
        }

        public override ObjectListResult Create() => new();

        public override bool Return(ObjectListResult obj)
        {
            if (obj.Count > _maxAllowedCapacity)
            {
                obj.Reset();
                return false;
            }

            obj.Reset();
            return true;
        }
    }
}
