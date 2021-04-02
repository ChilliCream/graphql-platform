using System;
using System.Threading.Tasks;
using GreenDonut;

namespace HotChocolate.Fetching
{
    public class AutoBatchScheduler
        : IBatchScheduler
    {
        public void Schedule(Func<ValueTask> dispatch) => dispatch();

        public static AutoBatchScheduler Default { get; } = new AutoBatchScheduler();
    }
}
