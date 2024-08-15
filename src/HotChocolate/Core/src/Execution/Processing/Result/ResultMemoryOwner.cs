namespace HotChocolate.Execution.Processing;

internal sealed class ResultMemoryOwner : IDisposable
{
    private readonly ResultPool _resultPool;
    private bool _disposed;

    public ResultMemoryOwner(ResultPool resultPool)
    {
        _resultPool = resultPool;
    }

    public ObjectResult? Data { get; set; }

    public List<ResultBucket<ObjectResult>> ObjectBuckets { get; } = [];

    public List<ResultBucket<ListResult>> ListBuckets { get; } = [];

    public void Dispose()
    {
        if (!_disposed)
        {
            _resultPool.Return(ObjectBuckets);
            _resultPool.Return(ListBuckets);
            _disposed = true;
        }
    }
}
