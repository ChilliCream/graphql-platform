using System;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace GreenDonut;

/// <summary>
/// Defines a batch dispatcher that immediately dispatches batch jobs.
/// </summary>
public class AutoBatchScheduler : IBatchScheduler
{
    private readonly ActionBlock<Func<ValueTask>> _actionBlock = new ActionBlock<Func<ValueTask>>(dispatch => dispatch());

    /// <summary>
    /// Schedules a new job to the dispatcher that is immediately executed.
    /// </summary>
    /// <param name="dispatch">
    /// The job that is being scheduled.
    /// </param>
    public async void Schedule(Func<ValueTask> dispatch)
        => _actionBlock.Post(dispatch);


    /// <summary>
    /// Gets the default instance if the <see cref="AutoBatchScheduler"/>.
    /// </summary>
    public static AutoBatchScheduler Default { get; } = new();
}
