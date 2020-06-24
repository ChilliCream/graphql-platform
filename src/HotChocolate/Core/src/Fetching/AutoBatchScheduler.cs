using System;
using GreenDonut;

namespace HotChocolate.Fetching
{
    /// <summary>
    /// Represents a batch dispatcher that dispatches immediately whenever a new batch arrives.
    /// </summary>
    public class AutoBatchScheduler
        : IBatchScheduler
        , IAutoBatchDispatcher
    {
        /// <inheritdoc/>
        public void Schedule(Action dispatch) => dispatch();
    }
}
