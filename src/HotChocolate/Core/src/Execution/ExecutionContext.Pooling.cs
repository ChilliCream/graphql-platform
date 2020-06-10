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
        private readonly TaskBacklog _taskBacklog;
        private readonly TaskStatistics _taskStatistics;

        public ExecutionContext(ObjectPool<ResolverTask> resolverTaskPool)
        {
            _taskStatistics = new TaskStatistics();
            _taskBacklog = new TaskBacklog(_taskStatistics, resolverTaskPool);
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
            _taskBacklog.Reset();
            _taskStatistics.Reset();
        }
    }
}
