using System;
using System.Threading.Tasks;

namespace HotChocolate.Fetching
{
    public class AutoBatchScheduler : IAutoBatchDispatcher
    {
        public void Schedule(Func<ValueTask> dispatch) => dispatch();

        public static AutoBatchScheduler Default { get; } = new();
    }
}
