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
        private readonly UnsortedChannel<ITask> _channel =
            new UnsortedChannel<ITask>(true);

        internal TaskBacklog(
            ITaskStatistics stats,
            ObjectPool<ResolverTask> resolverTaskPool)
        {
            _resolverTaskPool = resolverTaskPool;
            _stats = stats;
        }

        /// <inheritdoc/>
        public bool TryTake([NotNullWhen(true)] out ITask? task) =>
            _channel.Reader.TryRead(out task);

        /// <inheritdoc/>
        public async ValueTask<bool> WaitForTaskAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await _channel.Reader.WaitToReadAsync(cancellationToken);
            }
            catch (TaskCanceledException)
            {
                return false;
            }
        }

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

        public void Reset() => _channel.Reset();
    }
}
