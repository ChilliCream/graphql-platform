using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution
{
    internal sealed class ResolverContextPolicy
        : PooledObjectPolicy<ResolverContext>
    {
        public override ResolverContext Create()
        {
            return new ResolverContext();
        }

        public override bool Return(ResolverContext obj)
        {
            obj.Clean();
            return true;
        }
    }
}
