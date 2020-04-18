using System.Collections.Generic;
using HotChocolate.Execution.Utilities;

namespace HotChocolate.Execution
{
    internal sealed class Result : IResult
    {
        private readonly ResultPool _resultPool;

        public Result(ResultPool resultPool)
        {
            _resultPool = resultPool;
        }

        public IResultMap? Data { get; set; }

        public List<ObjectBuffer<ResultMap>> ResultMaps { get; } =
            new List<ObjectBuffer<ResultMap>>();

        public List<ObjectBuffer<ResultMapList>> ResultMapLists { get; } =
            new List<ObjectBuffer<ResultMapList>>();

        public void Dispose()
        {
            _resultPool.Return(ResultMaps);
            _resultPool.Return(ResultMapLists);
        }
    }
}
