using Microsoft.Extensions.ObjectPool;
using Mocha.Middlewares;

namespace Mocha;

/// <summary>
/// An object pool for <see cref="ReceiveContext"/> instances, reducing allocation overhead in high-throughput receive pipelines.
/// </summary>
/// <param name="maximumRetained">The maximum number of receive contexts to retain in the pool.</param>
public sealed class ReceiveContextPool(int maximumRetained = 256)
    : DefaultObjectPool<ReceiveContext>(new ReceiveContextPoolPolicy(), maximumRetained)
{
    private sealed class ReceiveContextPoolPolicy : IPooledObjectPolicy<ReceiveContext>
    {
        public ReceiveContext Create()
        {
            return new ReceiveContext();
        }

        public bool Return(ReceiveContext obj)
        {
            obj.Reset();

            return true;
        }
    }
}
