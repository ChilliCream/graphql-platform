using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Utilities
{
    /// <summary>
    ///  The task queue stores <see cref="ResolverTask"/> in a queue. 
    /// </summary>
    internal class TaskQueue : ITaskQueue
    {
        private readonly ObjectPool<ObjectBuffer<ResolverTask>> _resolverTaskPool;
        private readonly IOperationContext _operationContext;
        private readonly object _lockObject = new object();
        private readonly Queue<ObjectBuffer<ResolverTask>> _bufferQueue =
            new Queue<ObjectBuffer<ResolverTask>>();
        private readonly ConcurrentQueue<ResolverTask> _queue =
            new ConcurrentQueue<ResolverTask>();


        internal TaskQueue(
            IOperationContext operationContext,
            ObjectPool<ObjectBuffer<ResolverTask>> resolverTaskPool)
        {
            _resolverTaskPool = resolverTaskPool;
            _operationContext = operationContext;
            _bufferQueue.Enqueue(_resolverTaskPool.Get());
        }

        /// <inheritdoc/>
        public int Count { get => _queue.Count; }

        /// <inheritdoc/>
        public bool IsEmpty { get => _queue.IsEmpty; }

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
            // We first try to pop the next resolver task safely from the pool queue
            if (!_bufferQueue.Peek().TryPopSafe(out ResolverTask? resolverTask))
            {
                // In case the buffer is empty we need to enque a new one.
                // We lock so it is ensure that in any case only one buffer is rented from the pool
                lock (_lockObject)
                {
                    // check if another thread already enqueed a not empty buffer before we locked
                    // in this case we do not need to perform thread safe operations as we are
                    // already in a lock
                    if (!_bufferQueue.Peek().TryPop(out resolverTask))
                    {
                        _bufferQueue.Enqueue(_resolverTaskPool.Get());
                        resolverTask = _bufferQueue.Peek().Pop();
                    }
                }
            }

            resolverTask.Initialize(
                _operationContext,
                selection,
                resultMap,
                responseIndex,
                parent,
                path,
                scopedContextData);

            _queue.Enqueue(resolverTask);
        }

        public void Clear()
        {
            _queue.Clear();
            while (_bufferQueue.TryDequeue(out ObjectBuffer<ResolverTask>? buffer))
            {
                _resolverTaskPool.Return(buffer);
            }
        }
    }
}
