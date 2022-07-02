using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing;

internal sealed class ListResultPool : DefaultObjectPool<ResultBucket<ListResult>>
{
    public ListResultPool(int maximumRetained, int maxAllowedCapacity)
        : base(new BufferPolicy(maxAllowedCapacity), maximumRetained)
    {
    }

    private sealed class BufferPolicy : PooledObjectPolicy<ResultBucket<ListResult>>
    {
        private readonly ObjectPolicy _objectPolicy;

        public BufferPolicy(int maxAllowedCapacity)
        {
            _objectPolicy = new ObjectPolicy(maxAllowedCapacity);
        }

        public override ResultBucket<ListResult> Create()
            => new(16, _objectPolicy);

        public override bool Return(ResultBucket<ListResult> obj)
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
