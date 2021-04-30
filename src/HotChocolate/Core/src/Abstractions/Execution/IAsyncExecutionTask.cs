using System.Threading;
using System.Threading.Tasks;

namespace HotChocolate.Execution
{
    /*

     Strategy.Parallel => (backlog: 5, max: 5)
     Strategy.Serial
     Strategy.Pure => (backlog: 5, max: 5)

     DataLoader =>

     [ExecutionStrategy(Strategy.Serial)]
     public ValueTask<string> Fo()
     {
     }
     */

    /// <summary>
    /// Represents a task that shall be executed by the execution engine.
    /// </summary>
    public interface IAsyncExecutionTask
    {
        /// <summary>
        /// Defines if this task has completed.
        /// </summary>
        bool IsCompleted { get; }

        /// <summary>
        /// Defines if this task was canceled.
        /// </summary>
        bool IsCanceled { get; }

        /// <summary>
        /// Executes this task.
        /// </summary>
        ValueTask ExecuteAsync(CancellationToken cancellationToken);
    }

     /// <summary>
    /// Represents a task that shall be executed by the execution engine.
    /// </summary>
    public interface IExecutionTask
    {
        /// <summary>
        /// Defines if this task has completed.
        /// </summary>
        bool IsCompleted { get; }

        /// <summary>
        /// Defines if this task was canceled.
        /// </summary>
        bool IsCanceled { get; }

        /// <summary>
        /// Executes this task.
        /// </summary>
        void BeginExecute(CancellationToken cancellationToken);
    }

    /// <summary>
    /// Represents a task that shall be executed by the execution engine.
    /// </summary>
    public interface IPureExecutionTask
    {
        /// <summary>
        /// Defines if this task has completed.
        /// </summary>
        bool IsCompleted { get; }

        /// <summary>
        /// Defines if this task was canceled.
        /// </summary>
        bool IsCanceled { get; }

        /// <summary>
        /// Executes this task.
        /// </summary>
        void Execute(CancellationToken cancellationToken);
    }
}
