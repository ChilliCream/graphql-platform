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

    public List<ResultBucket<ObjectResult>> ObjectBuffers { get; } = new();

    public List<ResultBucket<ObjectListResult>> ResultMapLists { get; } = new();

    public List<ResultBucket<ListResult>> ResultLists { get; } = new();

    public void Dispose()
    {
        if (!_disposed)
        {
            _resultPool.Return(ObjectBuffers);
            _resultPool.Return(ResultMapLists);
            _resultPool.Return(ResultLists);
            _disposed = true;
        }
    }
}
