using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Utilities
{
    /// <inheritdoc/>
    internal class TaskQueue : ITaskBacklog
    {
        private readonly ObjectPool<ResolverTask> _resolverTaskPool;
        private readonly ITaskStatistics _stats;
        private Channel<ResolverTask> _channel = default!;

        internal TaskQueue(
            ITaskStatistics stats,
            ObjectPool<ResolverTask> resolverTaskPool)
        {
            _resolverTaskPool = resolverTaskPool;
            _stats = stats;
        }

        /// <inheritdoc/>
        public bool TryTake([NotNullWhen(true)] out ResolverTask? task) =>
            _channel.Reader.TryRead(out task);

        /// <inheritdoc/>
        public void Register(
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

            _stats.TaskCreated();
            
            _channel.Writer.TryWrite(resolverTask);
        }

        public void Initialize(Channel<ResolverTask> channel)
        {
            _channel = channel;
        }

        public void Clear()
        {
            _channel = default!;
        }
    }
}
