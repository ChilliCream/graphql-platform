using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace HotChocolate.Execution
{
    /// <summary>
    /// Provides the base implementation for tasks that are pure
    /// (have not side-effects and are synchronous).
    /// </summary>
    public abstract class PureExecutionTask : IExecutionTask
    {
        /// <inheritdoc />
        public ExecutionTaskKind Kind => ExecutionTaskKind.Pure;

        /// <inheritdoc />
        public IExecutionTask? Next { get; set; }

        /// <inheritdoc />
        public IExecutionTask? Previous { get; set; }

        /// <inheritdoc />
        public IExecutionTask? Parent { get; set; }

        /// <inheritdoc />
        public void BeginExecute(CancellationToken cancellationToken)
        {
            Execute(cancellationToken);
        }

        /// <inheritdoc />
        public Task WaitForCompletionAsync(CancellationToken cancellationToken) =>
            Task.CompletedTask;

        /// <summary>
        /// This execute method represents the work of this task.
        /// </summary>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        protected abstract void Execute(CancellationToken cancellationToken);
    }
}
