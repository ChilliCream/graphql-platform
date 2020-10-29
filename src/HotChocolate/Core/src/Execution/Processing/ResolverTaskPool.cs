using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing
{
    internal sealed class ResolverTaskPool
        : DefaultObjectPool<ResolverTask>
    {
        public ResolverTaskPool(
            int maximumRetained = 256)
            : base(new Policy(), maximumRetained)
        {
        }

        private class Policy : IPooledObjectPolicy<ResolverTask>
        {
            public ResolverTask Create() =>
                new ResolverTask();

            public bool Return(ResolverTask obj) =>
                obj.Reset();
        }
    }
}
