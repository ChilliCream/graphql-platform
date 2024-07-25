using System;
using System.Threading;
using GreenDonut;
using HotChocolate.Execution;

namespace HotChocolate.Fetching;

/// <summary>
/// The execution engine batch dispatcher.
/// </summary>
public interface IBatchDispatcher : IBatchScheduler
{
    /// <summary>
    /// Signals that a batch task was enqueued.
    /// </summary>
    event EventHandler TaskEnqueued;

    /// <summary>
    /// Gets the scheduler that is used to enqueue and monitor work.
    /// </summary>
    IExecutionTaskScheduler Scheduler { get; }

    /// <summary>
    /// Defines if the batch dispatcher shall dispatch tasks directly when they are enqueued.
    /// </summary>
    bool DispatchOnSchedule { get; set; }

    /// <summary>
    /// Begins dispatching batched tasks.
    /// </summary>
    void BeginDispatch(CancellationToken cancellationToken = default);
}
