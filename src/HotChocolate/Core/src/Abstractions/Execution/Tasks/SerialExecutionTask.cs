using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace HotChocolate.Execution
{
    /// <summary>
    /// Provides the base implementation for tasks that need to be executed serially.
    /// </summary>
    public abstract class SerialExecutionTask : IExecutionTask
    {
        private Task? _task;

        /// <summary>
        /// Gets the execution engine task context.
        /// </summary>
        protected abstract IExecutionTaskContext Context { get; }

        /// <inheritdoc />
        public ExecutionTaskKind Kind => ExecutionTaskKind.Serial;

        /// <inheritdoc />
        public bool IsCompleted => _task?.IsCompleted ?? false;

        /// <inheritdoc />
        public IExecutionTask? Parent { get; set; }

        /// <inheritdoc />
        public IExecutionTask? Next { get; set; }

        /// <inheritdoc />
        public IExecutionTask? Previous { get; set; }

        /// <inheritdoc />
        public object? State { get; set; }

        /// <inheritdoc />
        public bool IsSerial { get; set; }

        /// <inheritdoc />
        public void BeginExecute(CancellationToken cancellationToken)
        {
            _task = ExecuteInternalAsync(cancellationToken).AsTask();
        }

        /// <inheritdoc />
        public Task WaitForCompletionAsync(CancellationToken cancellationToken) =>
            _task ?? Task.CompletedTask;

        private async ValueTask ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            try
            {
                using (Context.Track(this))
                {
                    await ExecuteAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                Context.Completed(this);
            }
        }

        /// <summary>
        /// This execute method represents the work of this task.
        /// </summary>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        protected abstract ValueTask ExecuteAsync(CancellationToken cancellationToken);
    }
}
