using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// The work scheduler organizes the processing of request tasks.
/// </summary>
internal interface IWorkScheduler
{
    /// <summary>
    /// Gets the backlog of the task that shall be processed after
    /// all the main tasks have been executed.
    /// </summary>
    IDeferredWorkBacklog DeferredWork { get; }

    /// <summary>
    /// Defines if the execution is completed.
    /// </summary>
    bool IsCompleted { get; }

    /// <summary>
    /// Registers work with the task backlog.
    /// </summary>
    void Register(IExecutionTask task);

    /// <summary>
    /// Registers work with the task backlog.
    /// </summary>
    void Register(IReadOnlyList<IExecutionTask> tasks);

    /// <summary>
    /// Complete a task
    /// </summary>
    void Complete(IExecutionTask task);

    /// <summary>
    /// Defines if the given selection can be inlined.
    /// </summary>
    /// <param name="selection">
    /// The selection for which the execution state shall be determined.
    /// </param>
    /// <returns>
    /// <c>true</c> if the selection can be inlined.
    /// </returns>
    bool CanBeInlined(ISelection selection);

    /// <summary>
    /// Execute the work.
    /// </summary>
    Task ExecuteAsync();

    /// <summary>
    /// Resets the execution state.
    /// </summary>
    void Reset();
}
