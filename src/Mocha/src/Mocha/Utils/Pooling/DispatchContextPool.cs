using Microsoft.Extensions.ObjectPool;
using Mocha.Middlewares;

namespace Mocha;

/// <summary>
/// An object pool for <see cref="DispatchContext"/> instances, reducing allocation overhead in high-throughput dispatch pipelines.
/// </summary>
/// <param name="maximumRetained">The maximum number of dispatch contexts to retain in the pool.</param>
public sealed class DispatchContextPool(int maximumRetained = 256)
    : DefaultObjectPool<DispatchContext>(new DispatchContextPoolPolicy(), maximumRetained)
{
    private sealed class DispatchContextPoolPolicy : IPooledObjectPolicy<DispatchContext>
    {
        public DispatchContext Create()
        {
            return new DispatchContext();
        }

        public bool Return(DispatchContext obj)
        {
            obj.Reset();

            return true;
        }
    }
}
