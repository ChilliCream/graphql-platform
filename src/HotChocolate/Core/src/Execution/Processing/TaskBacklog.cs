using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;
using HotChocolate.Execution.Channels;

namespace HotChocolate.Execution.Processing
{
    /// <inheritdoc/>
    internal class TaskBacklog : ITaskBacklog
    {
        private readonly ObjectPool<ResolverTask> _resolverTaskPool;
        private readonly ObjectPool<PureResolverTask> _pureResolverTaskPool;
        private readonly ITaskStatistics _stats;
        private UnsortedChannel<IExecutionTask> _channel = new(true);

        internal TaskBacklog(
            ITaskStatistics stats,
            ObjectPool<ResolverTask> resolverTaskPool,
            ObjectPool<PureResolverTask> pureResolverTaskPool)
        {
            _resolverTaskPool = resolverTaskPool;
            _pureResolverTaskPool = pureResolverTaskPool;
            _stats = stats;

            _channel.NeedsMoreWorkers += (sender, args) =>
            {
                NeedsMoreWorker?.Invoke(sender, args);
            };
        }

        public event EventHandler<EventArgs>? NeedsMoreWorker;

        /// <inheritdoc/>
        public bool IsEmpty => _channel.IsEmpty;

        /// <inheritdoc/>
        public bool TryTake([NotNullWhen(true)] out IExecutionTask? task) =>
            _channel.Reader.TryRead(out task);

        /// <inheritdoc/>
        public ValueTask<bool> WaitForTaskAsync(CancellationToken cancellationToken) =>
            _channel.Reader.WaitToReadAsync(cancellationToken);

        /// <inheritdoc/>
        public void Register(ResolverTaskDefinition taskDefinition)
        {
            ResolverTaskBase resolverTask =
                taskDefinition.Selection.PureResolver is null
                    ? _resolverTaskPool.Get()
                    : _pureResolverTaskPool.Get();

            resolverTask.Initialize(
                taskDefinition.OperationContext,
                taskDefinition.Selection,
                taskDefinition.ResultMap,
                taskDefinition.ResponseIndex,
                taskDefinition.Parent,
                taskDefinition.Path,
                taskDefinition.ScopedContextData);

            Register(resolverTask);
        }

        public void Register(IExecutionTask task)
        {
            if (task is not PureResolverTask and not PureExecutionTask)
            {
                _stats.TaskCreated();
            }

            _channel.Writer.TryWrite(task);
        }

        public void Complete()
        {
            _channel.Writer.Complete();
        }

        public void Clear()
        {
            // _channel.NeedsMoreWorkers = null;
            NeedsMoreWorker = null;
            _channel = new UnsortedChannel<IExecutionTask>(true);
        }
    }
}
