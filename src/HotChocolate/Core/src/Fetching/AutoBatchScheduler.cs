using System;
using System.Threading.Tasks;

namespace HotChocolate.Fetching
{
    /// <summary>
    /// Defines a batch dispatcher that immediately dispatches batch jobs.
    /// </summary>
    public class AutoBatchScheduler : IAutoBatchDispatcher
    {
        /// <summary>
        /// Schedules a new job to the dispatcher that is immediately executed.
        /// </summary>
        /// <param name="dispatch">
        /// The job that is being scheduled.
        /// </param>
        public void Schedule(Func<ValueTask> dispatch) => dispatch();

        /// <summary>
        /// Gets the default instance if the <see cref="AutoBatchScheduler"/>.
        /// </summary>
        public static AutoBatchScheduler Default { get; } = new();
    }
}
