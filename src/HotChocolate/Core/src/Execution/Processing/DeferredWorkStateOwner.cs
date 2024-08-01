using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing;

internal sealed class DeferredWorkStateOwner : IDisposable
{
    private readonly ObjectPool<DeferredWorkState> _pool;
    private int _disposed;

    public DeferredWorkStateOwner( ObjectPool<DeferredWorkState> pool)
    {
        _pool = pool ?? throw new ArgumentNullException(nameof(pool));
        State = pool.Get();
    }

    public DeferredWorkState State { get; }

    public void Dispose()
    {
        if (_disposed == 0 && Interlocked.CompareExchange(ref _disposed, 1, 0) == 0)
        {
            _pool.Return(State);
        }
    }
}
