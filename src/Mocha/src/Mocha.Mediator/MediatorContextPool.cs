using Microsoft.Extensions.ObjectPool;

namespace Mocha.Mediator;

/// <summary>
/// An object pool for <see cref="MediatorContext"/> instances, reducing allocation
/// overhead in high-throughput dispatch pipelines.
/// </summary>
/// <param name="maximumRetained">The maximum number of contexts to retain in the pool.</param>
public sealed class MediatorContextPool(int maximumRetained = 256)
    : DefaultObjectPool<MediatorContext>(new MediatorContextPoolPolicy(), maximumRetained)
{
    private sealed class MediatorContextPoolPolicy : IPooledObjectPolicy<MediatorContext>
    {
        public MediatorContext Create()
        {
            return new MediatorContext();
        }

        public bool Return(MediatorContext obj)
        {
            obj.Reset();
            return true;
        }
    }
}
