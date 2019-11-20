using System.Buffers;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    internal static class ExecutionPools
    {
        private static readonly ArrayPool<ResolverContext> _contextPool =
            ArrayPool<ResolverContext>.Create(1024 * 1000, 512);
        private static readonly ArrayPool<Task> _taskPool =
            ArrayPool<Task>.Create(1024 * 1000, 512);

        public static ArrayPool<ResolverContext> ContextPool => _contextPool;

        public static ArrayPool<Task> TaskPool => _taskPool;
    }
}
