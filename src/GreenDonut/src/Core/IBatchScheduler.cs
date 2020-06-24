using System;

namespace GreenDonut
{
    /// <summary>
    /// Describes a component that schedules batches.
    /// </summary>
    public interface IBatchScheduler
    {
        /// <summary>
        /// Schedules a batch.
        /// </summary>
        /// <param name="dispatch">A delegate to dispatch one particular batch.</param>
        void Schedule(Action dispatch);
    }
}
