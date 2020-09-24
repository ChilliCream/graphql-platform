using System.Collections.Generic;

namespace HotChocolate.Execution.Processing
{
    internal sealed class ResultMemoryOwner : IResultMemoryOwner
    {
        private readonly ResultPool _resultPool;

        public ResultMemoryOwner(ResultPool resultPool)
        {
            _resultPool = resultPool;
        }

        public IResultMap? Data { get; set; }

        public List<ResultObjectBuffer<ResultMap>> ResultMaps { get; } =
            new List<ResultObjectBuffer<ResultMap>>();

        public List<ResultObjectBuffer<ResultMapList>> ResultMapLists { get; } =
            new List<ResultObjectBuffer<ResultMapList>>();

        public List<ResultObjectBuffer<ResultList>> ResultLists { get; } =
            new List<ResultObjectBuffer<ResultList>>();

        public void Dispose()
        {
            _resultPool.Return(ResultMaps);
            _resultPool.Return(ResultMapLists);
            _resultPool.Return(ResultLists);
        }
    }
}
