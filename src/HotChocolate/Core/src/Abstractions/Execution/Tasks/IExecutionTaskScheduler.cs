using System;
using System.Threading.Tasks;

namespace HotChocolate.Execution;

public interface IExecutionTaskScheduler
{
    event EventHandler AllTasksCompleted;

    /// <summary>
    /// Specified if the scheduler is still processing scheduled work.
    /// </summary>
    bool IsProcessing { get; }

    /// <summary>
    /// Schedules work to be processed.
    /// </summary>
    /// <param name="work">
    /// The work that shall be processed.
    /// </param>
    /// <returns>
    /// Returns a task representing the work.
    /// </returns>
    Task Schedule(Func<Task> work);
}
