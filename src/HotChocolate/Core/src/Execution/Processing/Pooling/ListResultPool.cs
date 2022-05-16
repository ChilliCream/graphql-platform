using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing.Pooling;

internal sealed class ListResultPool : DefaultObjectPool<ResultBuffer<ListResult>>
{
    public ListResultPool(int maximumRetained, int maxAllowedCapacity = 256)
        : base(new BufferPolicy(maxAllowedCapacity), maximumRetained)
    {
    }

    private sealed class BufferPolicy : PooledObjectPolicy<ResultBuffer<ListResult>>
    {
        private readonly ObjectPolicy _objectPolicy;

        public BufferPolicy(int maxAllowedCapacity)
        {
            _objectPolicy = new ObjectPolicy(maxAllowedCapacity);
        }

        public override ResultBuffer<ListResult> Create()
            => new(16, _objectPolicy);

        public override bool Return(ResultBuffer<ListResult> obj)
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
