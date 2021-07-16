using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing
{
    internal sealed class ResultMapPool
        : DefaultObjectPool<ResultObjectBuffer<ResultMap>>
    {
        public ResultMapPool(int maximumRetained)
            : base(new BufferPolicy(), maximumRetained)
        {
        }

        private class BufferPolicy : IPooledObjectPolicy<ResultObjectBuffer<ResultMap>>
        {
            private static readonly ResultMapPolicy _policy = new();

            public ResultObjectBuffer<ResultMap> Create() => new(16, _policy);

            public bool Return(ResultObjectBuffer<ResultMap> obj)
            {
                obj.Reset();
                return true;
            }
        }

        private class ResultMapPolicy : IPooledObjectPolicy<ResultMap>
        {
            public ResultMap Create() => new();

            public bool Return(ResultMap obj)
            {
                obj.Clear();
                return true;
            }
        }
    }
}
