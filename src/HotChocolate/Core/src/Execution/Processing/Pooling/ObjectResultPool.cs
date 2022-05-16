using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing.Pooling;

internal sealed class ObjectResultPool : DefaultObjectPool<ResultObjectBuffer<ObjectResult>>
{
    public ObjectResultPool(int maximumRetained, int maxAllowedCapacity = 256)
        : base(new BufferPolicy(maxAllowedCapacity), maximumRetained)
    {
    }

    private sealed class BufferPolicy : PooledObjectPolicy<ResultObjectBuffer<ObjectResult>>
    {
        private readonly ObjectPolicy _objectPolicy;

        public BufferPolicy(int maxAllowedCapacity)
        {
            _objectPolicy = new ObjectPolicy(maxAllowedCapacity);
        }

        public override ResultObjectBuffer<ObjectResult> Create()
            => new(16, _objectPolicy);

        public override bool Return(ResultObjectBuffer<ObjectResult> obj)
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

internal sealed class ListResultPool : DefaultObjectPool<ResultObjectBuffer<ListResult>>
{
    public ListResultPool(int maximumRetained, int maxAllowedCapacity = 256)
        : base(new BufferPolicy(maxAllowedCapacity), maximumRetained)
    {
    }

    private sealed class BufferPolicy : PooledObjectPolicy<ResultObjectBuffer<ListResult>>
    {
        private readonly ListResult _objectPolicy;

        public BufferPolicy(int maxAllowedCapacity)
        {
            _objectPolicy = new ObjectPolicy(maxAllowedCapacity);
        }

        public override ResultObjectBuffer<ListResult> Create()
            => new(16, _objectPolicy);

        public override bool Return(ResultObjectBuffer<ObjectResult> obj)
        {
            obj.Reset();
            return true;
        }
    }

    private sealed class ObjectPolicy : PooledObjectPolicy<ListResult>
    {
        private readonly int _maxAllowedCapacity;

        public ObjectPolicy(int maxAllowedCapacity)
        {
            _maxAllowedCapacity = maxAllowedCapacity;
        }

        public override ListResult Create() => new();

        public override bool Return(ListResult obj)
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
