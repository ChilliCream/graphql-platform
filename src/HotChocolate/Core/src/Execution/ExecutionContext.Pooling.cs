using Microsoft.Extensions.ObjectPool;
using HotChocolate.Execution.Utilities;
using HotChocolate.Fetching;
using System.Threading;
using System.Threading.Tasks;
using System;
using System.Threading.Channels;
using HotChocolate.Execution.Channels;

namespace HotChocolate.Execution
{
    internal partial class ExecutionContext
        : IExecutionContext
    {
        private readonly TaskQueue _taskQueue;
        private readonly TaskStatistics _taskStatistics;
        private Channel<ResolverTask> _channel = default!;

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
            _channel = new UnsortedChannel<ResolverTask>(true);
            _taskQueue.Initialize(_channel);
        }

        public void Reset()
        {
            BatchDispatcher.TaskEnqueued -= BatchDispatcherEventHandler;
            BatchDispatcher = default!;
            _taskQueue.Clear();
            _taskStatistics.Clear();
            _channel = default!;
        }
    }
}
