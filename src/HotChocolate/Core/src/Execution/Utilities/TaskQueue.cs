using System;
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
        private readonly IOperationContext _operationContext;
        private readonly ConcurrentQueue<ResolverTask> _queue =
            new ConcurrentQueue<ResolverTask>();

        public event EventHandler? TaskEnqueued;

        internal TaskQueue(
            IOperationContext operationContext,
            ObjectPool<ResolverTask> resolverTaskPool)
        {
            _resolverTaskPool = resolverTaskPool;
            _operationContext = operationContext;
        }

        /// <inheritdoc/>
        public int Count => _queue.Count;

        /// <inheritdoc/>
        public bool IsEmpty => _queue.IsEmpty;

        /// <inheritdoc/>
        public bool TryDequeue([NotNullWhen(true)] out ResolverTask? task) =>
            _queue.TryDequeue(out task);

        /// <inheritdoc/>
        public void Enqueue(
            IPreparedSelection selection,
            int responseIndex,
            ResultMap resultMap,
            object? parent,
            Path path,
            IImmutableDictionary<string, object?> scopedContextData)
        {
            ResolverTask resolverTask = _resolverTaskPool.Get();

            resolverTask.Initialize(
                _operationContext,
                selection,
                resultMap,
                responseIndex,
                parent,
                path,
                scopedContextData);

            _queue.Enqueue(resolverTask);

            TaskEnqueued?.Invoke(this, EventArgs.Empty);
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
