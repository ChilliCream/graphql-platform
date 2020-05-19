using System.Threading.Tasks;
using HotChocolate.Execution.Utilities;
using HotChocolate.Fetching;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution
{
    internal partial class ExecutionContext 
        : IExecutionContext
    {
        private readonly TaskQueue _taskQueue;
        private readonly TaskStatistics _taskStatistics;
        private readonly object _engineLock = new object();
        private TaskCompletionSource<bool>? _waitForEngineTask;

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
            ResetTaskSource();
        }
    }
}
