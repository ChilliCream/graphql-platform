using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing
{
    internal sealed class ResultMapListPool
        : DefaultObjectPool<ResultObjectBuffer<ResultMapList>>
    {
        public ResultMapListPool(int maximumRetained)
            : base(new BufferPolicy(), maximumRetained)
        {
        }

        private class BufferPolicy : IPooledObjectPolicy<ResultObjectBuffer<ResultMapList>>
        {
            private static readonly ResultMapListPolicy _policy = new ResultMapListPolicy();

            public ResultObjectBuffer<ResultMapList> Create() =>
                new ResultObjectBuffer<ResultMapList>(16, _policy);

            public bool Return(ResultObjectBuffer<ResultMapList> obj)
            {
                obj.Reset();
                return true;
            }
        }

        private class ResultMapListPolicy : IPooledObjectPolicy<ResultMapList>
        {
            public ResultMapList Create() => new ResultMapList();

            public bool Return(ResultMapList obj)
            {
                obj.Clear();
                return true;
            }
        }
    }
}
