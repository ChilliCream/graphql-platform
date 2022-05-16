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

    public List<ResultBucket<ObjectResult>> ObjectBuckets { get; } = new();

    public List<ResultBucket<ObjectListResult>> ObjectListBuckets { get; } = new();

    public List<ResultBucket<ListResult>> ListBuckets { get; } = new();

    public void Dispose()
    {
        if (!_disposed)
        {
            _resultPool.Return(ObjectBuckets);
            _resultPool.Return(ObjectListBuckets);
            _resultPool.Return(ListBuckets);
            _disposed = true;
        }
    }
}
