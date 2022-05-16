using System;
using System.Collections.Generic;

namespace HotChocolate.Execution.Processing.Pooling;

internal sealed class ResultMemoryOwner : IDisposable
{
    private readonly ResultPool _resultPool;
    private bool _disposed;

    public ResultMemoryOwner(ResultPool resultPool)
    {
        _resultPool = resultPool;
    }

    public ObjectResult? Data { get; set; }

    public List<ResultBuffer<ObjectResult>> ResultMaps { get; } = new();

    public List<ResultBuffer<ObjectListResult>> ResultMapLists { get; } = new();

    public List<ResultBuffer<ListResult>> ResultLists { get; } = new();

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
