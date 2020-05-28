using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Utilities
{
    /// <inheritdoc/>
    internal class TaskQueue : ITaskQueue
    {
        private readonly ObjectPool<ResolverTask> _resolverTaskPool;
        private readonly ITaskStatistics _stats;
        private readonly ConcurrentBag<ResolverTask> _queue =
            new ConcurrentBag<ResolverTask>();

        internal TaskQueue(
            ITaskStatistics stats,
            ObjectPool<ResolverTask> resolverTaskPool)
        {
            _resolverTaskPool = resolverTaskPool;
            _stats = stats;
        }

        /// <inheritdoc/>
        public int Count => _queue.Count;

        /// <inheritdoc/>
        public bool IsEmpty => _queue.IsEmpty;

        /// <inheritdoc/>
        public bool TryDequeue([NotNullWhen(true)] out ResolverTask? task) =>
            _queue.TryTake(out task);

        /// <inheritdoc/>
        public void Enqueue(
            IOperationContext operationContext,
            IPreparedSelection selection,
            int responseIndex,
            ResultMap resultMap,
            object? parent,
            Path path,
            IImmutableDictionary<string, object?> scopedContextData)
        {
            ResolverTask resolverTask = _resolverTaskPool.Get();

            resolverTask.Initialize(
                operationContext,
                selection,
                resultMap,
                responseIndex,
                parent,
                path,
                scopedContextData);

            _stats.DoWork(resolverTask);
        }

        public void Clear()
        {
#if NETSTANDARD2_0
            while (_queue.TryDequeue(out _))
            {
            }
#else
            _queue.Clear();
#endif
        }
    }
}
