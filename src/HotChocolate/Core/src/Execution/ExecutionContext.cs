using System;
using Microsoft.Extensions.ObjectPool;
using HotChocolate.Execution.Utilities;
using HotChocolate.Fetching;
using System.Threading.Tasks;
using System.Threading;

namespace HotChocolate.Execution
{
    internal partial class ExecutionContext
        : IExecutionContext
    {
        public ITaskQueue Tasks => _taskQueue;

        public ITaskStatistics TaskStats => _taskStatistics;

        public bool IsCompleted => TaskStats.IsCompleted;

        public ObjectPool<ResolverTask> TaskPool { get; }

        public IBatchDispatcher BatchDispatcher { get; private set; } = default!;

        public Task WaitAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private void SetEngineState()
        {
            if (TaskStats.NewTasks == 0 && BatchDispatcher.HasTasks)
            {
                BatchDispatcher.Dispatch();
            }
        }

        private void BatchDispatcherEventHandler(
            object? source, EventArgs args) =>
            SetEngineState();

        private void TaskStatisticsEventHandler(
            object? source, EventArgs args) =>
            SetEngineState();
    }
}
