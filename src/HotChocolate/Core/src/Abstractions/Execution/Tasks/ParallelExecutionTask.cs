using System;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace HotChocolate.Execution
{
    /// <summary>
    /// Provides the base implementation for a parallel executable task.
    /// </summary>
    public abstract class ParallelExecutionTask : IExecutionTask
    {
        private ExecutionTaskStatus _completionStatus = ExecutionTaskStatus.Completed;
        private Task? _task;

        /// <summary>
        /// Gets the execution engine task context.
        /// </summary>
        protected abstract IExecutionTaskContext Context { get; }

        /// <inheritdoc />
        public ExecutionTaskKind Kind => ExecutionTaskKind.Parallel;

        /// <inheritdoc />
        public ExecutionTaskStatus Status { get; private set; }

        /// <inheritdoc />
        public IExecutionTask? Next { get; set; }

        /// <inheritdoc />
        public IExecutionTask? Previous { get; set; }

        /// <inheritdoc />
        public object? State { get; set; }

        /// <inheritdoc />
        public bool IsSerial { get; set; }

        /// <inheritdoc />
        public bool IsRegistered { get; set; }

        /// <inheritdoc />
        public void BeginExecute(CancellationToken cancellationToken)
        {
            Status = ExecutionTaskStatus.Running;
            _task = ExecuteInternalAsync(cancellationToken).AsTask();
        }

        /// <inheritdoc />
        public Task WaitForCompletionAsync(CancellationToken cancellationToken)
            => _task ?? Task.CompletedTask;

        private async ValueTask ExecuteInternalAsync(CancellationToken cancellationToken)
        {
            try
            {
                using (Context.Track(this))
                {
                    await ExecuteAsync(cancellationToken).ConfigureAwait(false);
                }

                Status = _completionStatus;
            }
            catch (OperationCanceledException)
            {
                Status = ExecutionTaskStatus.Faulted;

                // If we run into this exception the request was aborted.
                // In this case we do nothing and just return.
            }
            catch (Exception ex)
            {
                Status = ExecutionTaskStatus.Faulted;

                if (cancellationToken.IsCancellationRequested)
                {
                    // if cancellation is request we do no longer report errors to the
                    // operation context.
                    return;
                }

                Context.ReportError(this, ex);
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

        /// <summary>
        /// Completes the task as faulted.
        /// </summary>
        protected void Faulted()
        {
            _completionStatus = ExecutionTaskStatus.Faulted;
        }

        /// <summary>
        /// Resets the state of this task in case the task object is reused.
        /// </summary>
        protected void Reset()
        {
            _task = null;
            Next = null;
            Previous = null;
            State = null;
            IsSerial = false;
            IsRegistered = false;
            _completionStatus = ExecutionTaskStatus.Completed;
            Status = ExecutionTaskStatus.WaitingToRun;
        }
    }
}
