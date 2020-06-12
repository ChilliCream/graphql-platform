using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;
using HotChocolate.Execution.Channels;

namespace HotChocolate.Execution.Utilities
{
    /// <inheritdoc/>
    internal class TaskBacklog : ITaskBacklog
    {
        private readonly ObjectPool<ResolverTask> _resolverTaskPool;
        private readonly ITaskStatistics _stats;
        private UnsortedChannel<ITask> _channel =
            new UnsortedChannel<ITask>(true);

        internal TaskBacklog(
            ITaskStatistics stats,
            ObjectPool<ResolverTask> resolverTaskPool)
        {
            _resolverTaskPool = resolverTaskPool;
            _stats = stats;
        }

        /// <inheritdoc/>
        public bool IsEmpty => _channel.IsEmpty;

        /// <inheritdoc/>
        public bool TryTake([NotNullWhen(true)] out ITask? task) =>
            _channel.Reader.TryRead(out task);

        /// <inheritdoc/>
        public ValueTask<bool> WaitForTaskAsync(CancellationToken cancellationToken) =>
            _channel.Reader.WaitToReadAsync(cancellationToken);

        /// <inheritdoc/>
        public void Register(ResolverTaskDefinition taskDefinition)
        {
            ResolverTask resolverTask = _resolverTaskPool.Get();

            resolverTask.Initialize(
                taskDefinition.OperationContext,
                taskDefinition.Selection,
                taskDefinition.ResultMap,
                taskDefinition.ResponseIndex,
                taskDefinition.Parent,
                taskDefinition.Path,
                taskDefinition.ScopedContextData);

            _stats.TaskCreated();
            _channel.Writer.TryWrite(resolverTask);
        }

        public void Complete() => _channel.Writer.Complete();

        public void Reset()
        {
            _channel = new UnsortedChannel<ITask>(true);
        }
    }
}
