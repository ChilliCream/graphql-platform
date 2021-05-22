using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace HotChocolate.Execution
{
    /// <summary>
    /// This interface represents a task that can be executed by the execution engine.
    /// </summary>
    public interface IExecutionTask
    {
        /// <summary>
        /// Defines the kind of task.
        /// The task kind is used to apply the correct execution strategy.
        /// </summary>
        ExecutionTaskKind Kind { get; }

        /// <summary>
        /// Defines if this task is completed.
        /// </summary>
        bool IsCompleted { get; }

        /// <summary>
        /// If this task is being executed within another task the parent property exposes the
        /// owner task. Child tasks are not tracked within the execution engine.
        /// </summary>
        IExecutionTask? Parent { get; set; }

        /// <summary>
        /// Next and previous are properties that are used by the execution engine to
        /// track the execution state.
        /// </summary>
        IExecutionTask? Next { get; set; }

        /// <summary>
        /// Next and previous are properties that are used by the execution engine to
        /// track the execution state.
        /// </summary>
        IExecutionTask? Previous { get; set; }

        /// <summary>
        /// This property is set by the execution engine and stores the execution state on it.
        /// </summary>
        object? State { get; set; }

        /// <summary>
        /// This property is set by the execution engine defines if the task needs to be
        /// executed in a serial context.
        /// </summary>
        bool IsSerial { get; set; }

        /// <summary>
        /// Begins executing this task.
        /// </summary>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        void BeginExecute(CancellationToken cancellationToken);

        /// <summary>
        /// The running task can be awaited to track completion of this particular task.
        /// </summary>
        /// <param name="cancellationToken">
        /// The cancellation token.
        /// </param>
        Task WaitForCompletionAsync(CancellationToken cancellationToken);
    }
}
