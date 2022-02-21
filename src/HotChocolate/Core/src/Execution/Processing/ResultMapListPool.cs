using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing;

internal sealed class ResultMapListPool : DefaultObjectPool<ResultObjectBuffer<ResultMapList>>
{
    public ResultMapListPool(int maximumRetained)
        : base(new BufferPolicy(), maximumRetained)
    {
    }

    private sealed class BufferPolicy : IPooledObjectPolicy<ResultObjectBuffer<ResultMapList>>
    {
        private static readonly ResultMapListPolicy _policy = new();

        public ResultObjectBuffer<ResultMapList> Create() => new(16, _policy);

        public bool Return(ResultObjectBuffer<ResultMapList> obj)
        {
            obj.Reset();
            return true;
        }
    }

    private sealed class ResultMapListPolicy : IPooledObjectPolicy<ResultMapList>
    {
        public ResultMapList Create() => new();

        public bool Return(ResultMapList obj)
        {
            obj.Clear();
            return true;
        }
    }
}
