using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution.Channels;

namespace HotChocolate.Execution.Processing
{
    /// <inheritdoc/>
    internal class TaskBacklog : ITaskBacklog
    {
        private readonly WorkQueue _work = new();

        /// <inheritdoc/>
        public event EventHandler<EventArgs>? BackPressureLimitExceeded;

        /// <inheritdoc/>
        public bool IsEmpty => _work.IsEmpty;

        /// <inheritdoc/>
        public bool TryTake([NotNullWhen(true)] out IExecutionTask? task) =>
            _work.TryTake(out task);

        /// <inheritdoc/>
        public Task WaitForWorkAsync(CancellationToken cancellationToken)
        {
            if (_work.TryPeekInProgress(out IExecutionTask executionTask))
            {
                return executionTask.WaitForCompletionAsync(cancellationToken);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public void Register(IExecutionTask task)
        {
            if (task is null)
            {
                throw new ArgumentNullException(nameof(task));
            }
            _work.Push(task);
        }
    }
}
