using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution
{
    internal sealed class ResolverContextListPolicy
        : PooledObjectPolicy<List<ResolverContext>>
    {
        public override List<ResolverContext> Create()
        {
            return new List<ResolverContext>();
        }

        public override bool Return(List<ResolverContext> obj)
        {
            obj.Clear();
            return true;
        }
    }
}
