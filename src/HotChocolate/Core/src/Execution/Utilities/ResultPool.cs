using System.Collections.Generic;
using HotChocolate.Execution.Utilities;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution
{
    internal sealed class ResultPool
    {
        private readonly ObjectPool<ObjectBuffer<ResultMap>> _resultMapPool;
        private readonly ObjectPool<ObjectBuffer<ResultMapList>> _resultMapListPool;
        private readonly ObjectPool<ObjectBuffer<ResultList>> _resultListPool;

        public ResultPool(
            ObjectPool<ObjectBuffer<ResultMap>> resultMapPool,
            ObjectPool<ObjectBuffer<ResultMapList>> resultMapListPool,
            ObjectPool<ObjectBuffer<ResultList>> resultListPool)
        {
            _resultMapPool = resultMapPool;
            _resultMapListPool = resultMapListPool;
            _resultListPool = resultListPool;
        }

        public ObjectBuffer<ResultMap> GetResultMap()
        {
            return _resultMapPool.Get();
        }

        public ObjectBuffer<ResultMapList> GetResultMapList()
        {
            return _resultMapListPool.Get();
        }

        public ObjectBuffer<ResultList> GetResultList()
        {
            return _resultListPool.Get();
        }

        public void Return(IList<ObjectBuffer<ResultMap>> buffers)
        {
            for (int i = 0; i < buffers.Count; i++)
            {
                _resultMapPool.Return(buffers[i]);
            }
        }

        public void Return(IList<ObjectBuffer<ResultMapList>> buffers)
        {
            for (int i = 0; i < buffers.Count; i++)
            {
                _resultMapListPool.Return(buffers[i]);
            }
        }

        public void Return(IList<ObjectBuffer<ResultList>> buffers)
        {
            for (int i = 0; i < buffers.Count; i++)
            {
                _resultListPool.Return(buffers[i]);
            }
        }
    }
}
