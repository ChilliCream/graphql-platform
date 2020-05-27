using Microsoft.Extensions.ObjectPool;
using HotChocolate.Execution.Utilities;
using HotChocolate.Fetching;

namespace HotChocolate.Execution
{
    internal partial class ExecutionContext
        : IExecutionContext
    {
        private readonly TaskQueue _taskQueue;
        private readonly TaskStatistics _taskStatistics;

        public ExecutionContext(ObjectPool<ResolverTask> resolverTaskPool)
        {
            _taskStatistics = new TaskStatistics();
            _taskQueue = new TaskQueue(_taskStatistics, resolverTaskPool);
            TaskPool = resolverTaskPool;
            TaskStats.StateChanged += TaskStatisticsEventHandler;
        }

        public void Initialize(IBatchDispatcher batchDispatcher)
        {
            BatchDispatcher = batchDispatcher;
            BatchDispatcher.TaskEnqueued += BatchDispatcherEventHandler;
        }

        public void Reset()
        {
            BatchDispatcher.TaskEnqueued -= BatchDispatcherEventHandler;
            BatchDispatcher = default!;
            _taskQueue.Clear();
            _taskStatistics.Clear();
        }
    }
}
