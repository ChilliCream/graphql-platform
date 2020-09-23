using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing
{
    internal sealed class ResultListPool
        : DefaultObjectPool<ResultObjectBuffer<ResultList>>
    {
        public ResultListPool(int maximumRetained) 
            : base(new BufferPolicy(), maximumRetained)
        {
        }

        private class BufferPolicy : IPooledObjectPolicy<ResultObjectBuffer<ResultList>>
        {
            private static readonly ResultMapPolicy _policy = new ResultMapPolicy();

            public ResultObjectBuffer<ResultList> Create() =>
                new ResultObjectBuffer<ResultList>(16, _policy);

            public bool Return(ResultObjectBuffer<ResultList> obj)
            {
                obj.Reset();
                return true;
            }
        }

        private class ResultMapPolicy : IPooledObjectPolicy<ResultList>
        {
            public ResultList Create() => new ResultList();

            public bool Return(ResultList obj)
            {
                obj.Clear();
                return true;
            }
        }
    }
}
