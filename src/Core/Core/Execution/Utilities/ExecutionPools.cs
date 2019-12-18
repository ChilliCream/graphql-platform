using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution
{
    internal static class ExecutionPools
    {
        public static DefaultObjectPool<ResolverContext> ResolverContext { get; } =
            new DefaultObjectPool<ResolverContext>(
                new ResolverContextPooledObjectPolicy(),
                128);


        public static DefaultObjectPool<List<ResolverContext>> ResolverContextList { get; } =
            new DefaultObjectPool<List<ResolverContext>>(
                new ListPooledObjectPolicy<ResolverContext>(),
                128);

        public static DefaultObjectPool<List<Task>> TaskList { get; } =
            new DefaultObjectPool<List<Task>>(
                new ListPooledObjectPolicy<Task>(),
                32);
    }

    internal class ListPooledObjectPolicy<T> : PooledObjectPolicy<List<T>>
    {
        public override List<T> Create() => new List<T>();

        public override bool Return(List<T> obj)
        {
            obj.Clear();
            return true;
        }
    }

    internal class ResolverContextPooledObjectPolicy : PooledObjectPolicy<ResolverContext>
    {
        public override ResolverContext Create() => new ResolverContext();

        public override bool Return(ResolverContext obj)
        {
            obj.Clean();
            return true;
        }
    }
}
