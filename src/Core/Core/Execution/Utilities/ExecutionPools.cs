using System.Buffers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    internal static class ExecutionPools
    {
        public static ConcurrentObjectPool<ResolverContext> ResolverContext { get; } =
            new ConcurrentObjectPool<ResolverContext>(
                () => new ResolverContext(),
                t => t.Clean(),
                512);

        public static ConcurrentObjectPool<List<ResolverContext>> ResolverContextList { get; } =
            new ConcurrentObjectPool<List<ResolverContext>>(
                () => new List<ResolverContext>(),
                t => t.Clear(),
                256);

        public static ConcurrentObjectPool<List<Task>> TaskList { get; } =
            new ConcurrentObjectPool<List<Task>>(
                () => new List<Task>(),
                t => t.Clear(),
                256);
    }
}
