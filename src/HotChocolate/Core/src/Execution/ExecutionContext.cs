using System;
using Microsoft.Extensions.ObjectPool;
using HotChocolate.Execution.Utilities;
using HotChocolate.Fetching;

namespace HotChocolate.Execution
{
    internal partial class ExecutionContext
        : IExecutionContext
    {
        private readonly object _sync = new object();

        public ITaskBacklog TaskBacklog => _taskBacklog;

        public ITaskStatistics TaskStats => _taskStatistics;

        public bool IsCompleted => TaskStats.IsCompleted;

        public ObjectPool<ResolverTask> TaskPool { get; }

        public IBatchDispatcher BatchDispatcher { get; private set; } = default!;

        private void SetEngineState()
        {
            if (TaskBacklog.IsEmpty && BatchDispatcher.HasTasks)
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

        private void OnCompleted(
            object? source, 
            EventArgs args) =>
            _taskBacklog.Complete();
    }
}
