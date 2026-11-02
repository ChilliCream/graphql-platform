using Microsoft.Extensions.ObjectPool;

namespace Mocha.Mediator;

internal sealed class MediatorPools(ObjectPool<MediatorContext> mediatorContextPool) : IMediatorPools
{
    public ObjectPool<MediatorContext> MediatorContext => mediatorContextPool;
}
