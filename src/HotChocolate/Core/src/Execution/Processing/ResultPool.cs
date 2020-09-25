using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing
{
    internal sealed class ResultPool
    {
        private readonly ObjectPool<ResultObjectBuffer<ResultMap>> _resultMapPool;
        private readonly ObjectPool<ResultObjectBuffer<ResultMapList>> _resultMapListPool;
        private readonly ObjectPool<ResultObjectBuffer<ResultList>> _resultListPool;

        public ResultPool(
            ObjectPool<ResultObjectBuffer<ResultMap>> resultMapPool,
            ObjectPool<ResultObjectBuffer<ResultMapList>> resultMapListPool,
            ObjectPool<ResultObjectBuffer<ResultList>> resultListPool)
        {
            _resultMapPool = resultMapPool;
            _resultMapListPool = resultMapListPool;
            _resultListPool = resultListPool;
        }

        public ResultObjectBuffer<ResultMap> GetResultMap()
        {
            return _resultMapPool.Get();
        }

        public ResultObjectBuffer<ResultMapList> GetResultMapList()
        {
            return _resultMapListPool.Get();
        }

        public ResultObjectBuffer<ResultList> GetResultList()
        {
            return _resultListPool.Get();
        }

        public void Return(IList<ResultObjectBuffer<ResultMap>> buffers)
        {
            for (int i = 0; i < buffers.Count; i++)
            {
                _resultMapPool.Return(buffers[i]);
            }
        }

        public void Return(IList<ResultObjectBuffer<ResultMapList>> buffers)
        {
            for (int i = 0; i < buffers.Count; i++)
            {
                _resultMapListPool.Return(buffers[i]);
            }
        }

        public void Return(IList<ResultObjectBuffer<ResultList>> buffers)
        {
            for (int i = 0; i < buffers.Count; i++)
            {
                _resultListPool.Return(buffers[i]);
            }
        }
    }
}
