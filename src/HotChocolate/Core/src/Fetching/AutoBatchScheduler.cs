using System;
using GreenDonut;

namespace HotChocolate.Fetching
{
    public class AutoBatchScheduler
        : IBatchScheduler
        , IAutoBatchDispatcher
    {
        public void Schedule(Action dispatch) => dispatch();

        public static AutoBatchScheduler Default { get; } = new AutoBatchScheduler();
    }
}
