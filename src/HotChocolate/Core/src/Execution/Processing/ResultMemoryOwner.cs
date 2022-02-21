using System.Collections.Generic;

namespace HotChocolate.Execution.Processing;

internal sealed class ResultMemoryOwner : IResultMemoryOwner
{
    private readonly ResultPool _resultPool;
    private bool _disposed;

    public ResultMemoryOwner(ResultPool resultPool)
    {
        _resultPool = resultPool;
    }

    public IResultMap? Data { get; set; }

    public List<ResultObjectBuffer<ResultMap>> ResultMaps { get; } = new();

    public List<ResultObjectBuffer<ResultMapList>> ResultMapLists { get; } = new();

    public List<ResultObjectBuffer<ResultList>> ResultLists { get; } = new();

    public void Dispose()
    {
        if (!_disposed)
        {
            _resultPool.Return(ResultMaps);
            _resultPool.Return(ResultMapLists);
            _resultPool.Return(ResultLists);
            _disposed = true;
        }
    }
}
