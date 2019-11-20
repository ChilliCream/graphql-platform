using System.Buffers;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution
{
    internal static class ExecutionPools
    {
        private static readonly ArrayPool<ResolverContext> _contextPool =
            ArrayPool<ResolverContext>.Create(1024 * 1000, 512);
        private static readonly ArrayPool<Task> _taskPool =
            ArrayPool<Task>.Create(1024 * 1000, 512);

        private static readonly DefaultObjectPool<List<ResolverContext>> _contextListPool =
            new DefaultObjectPool<List<ResolverContext>>(new ResolverContextListPolicy(), 1024);

        public static ArrayPool<ResolverContext> ContextPool => _contextPool;

        public static ArrayPool<Task> TaskPool => _taskPool;

        public static DefaultObjectPool<List<ResolverContext>> ContextListPool => _contextListPool;
    }
}
