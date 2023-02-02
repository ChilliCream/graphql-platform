using System;
using System.Threading;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing;

internal sealed class DeferredWorkStateOwner : IDisposable
{
    private readonly ObjectPool<DeferredWorkState> _statePool;
    private int _disposed;

    public DeferredWorkStateOwner(DeferredWorkState state, ObjectPool<DeferredWorkState> statePool)
    {
        State = state ?? throw new ArgumentNullException(nameof(state));
        _statePool = statePool ?? throw new ArgumentNullException(nameof(statePool));
    }

    public DeferredWorkState State { get; }

    public void Dispose()
    {
        if (_disposed == 0 && Interlocked.CompareExchange(ref _disposed, 1, 0) == 0)
        {
            _statePool.Return(State);
        }
    }
}
