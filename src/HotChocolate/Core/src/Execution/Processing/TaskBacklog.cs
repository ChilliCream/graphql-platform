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
        public event EventHandler<EventArgs>? BacklogEmpty;

        /// <inheritdoc/>
        public bool IsEmpty => _work.IsEmpty;

        public bool IsRunning => _work.IsRunning;

        public TaskBacklog()
        {
            _work.BacklogEmpty += (sender, args) => BacklogEmpty?.Invoke(sender, args);
        }

        /// <inheritdoc/>
        public bool TryTake([NotNullWhen(true)] out IExecutionTask? task) =>
            _work.TryTake(out task);

        /// <inheritdoc/>
        public Task WaitForWorkAsync(CancellationToken cancellationToken)
        {
            if (_work.TryPeekInProgress(out IExecutionTask? executionTask))
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

            //TODO: This does obviously not work, we overcommit massively because we do not give the
            //      tasks a chance to process
            if (_work.Push(task) > 15)
            {
                BackPressureLimitExceeded?.Invoke(null, EventArgs.Empty);
            }
        }

        /// <inheritdoc/>
        public void Complete(IExecutionTask task)
        {
            if (task is null)
            {
                throw new ArgumentNullException(nameof(task));
            }
            _work.Complete(task);
        }

        public void Clear()
        {
            _work.ClearUnsafe();
            BackPressureLimitExceeded = null;
        }
    }
}
