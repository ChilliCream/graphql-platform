using HotChocolate.Execution.DependencyInjection;
using HotChocolate.Execution.Processing;
using Microsoft.Extensions.ObjectPool;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// The deferred <see cref="DeferredWorkStateOwnerFactory"/> is injected as a scoped services and
/// preserves the <see cref="DeferredWorkStateOwner"/> instance it creates.
///
/// This is done so that the executions running on one service scope share the deferred execution
/// state between each other.
///
/// <see cref="DeferredWorkStateOwner"/> is disposable and will be disposed with the request scope.
/// </summary>
internal sealed class DeferredWorkStateOwnerFactory : IFactory<DeferredWorkStateOwner>
{
    private readonly ObjectPool<DeferredWorkState> _pool;
    private DeferredWorkStateOwner? _owner;

    public DeferredWorkStateOwnerFactory(ObjectPool<DeferredWorkState> pool)
    {
        _pool = pool;
    }

    public DeferredWorkStateOwner Create()
    {
        if (_owner is null)
        {
            lock (_pool)
            {
                if (_owner is null)
                {
                    _owner = new DeferredWorkStateOwner(_pool);
                }
            }
        }

        return _owner;
    }
}
