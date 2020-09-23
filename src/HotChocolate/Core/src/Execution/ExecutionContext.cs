using System;
using Microsoft.Extensions.ObjectPool;
using HotChocolate.Execution.Processing;
using HotChocolate.Fetching;

namespace HotChocolate.Execution
{
    internal partial class ExecutionContext
        : IExecutionContext
    {
        private readonly object _sync = new object();

        public ITaskBacklog TaskBacklog => _taskBacklog;

        public IDeferredTaskBacklog DeferredTaskBacklog { get; } = 
            new DeferredTaskBacklog();

        public ITaskStatistics TaskStats => _taskStatistics;

        public bool IsCompleted => TaskStats.IsCompleted;

        public ObjectPool<ResolverTask> TaskPool { get; }

        public IBatchDispatcher BatchDispatcher { get; private set; } = default!;

        private void TryDispatchBatches()
        {
            if (TaskBacklog.IsEmpty && BatchDispatcher.HasTasks)
            {
                BatchDispatcher.Dispatch(Register);
            }

            void Register(IExecutionTaskDefinition taskDefinition)
            {
                TaskBacklog.Register(taskDefinition.Create(_taskContext));
            }
        }

        private void BatchDispatcherEventHandler(
            object? source, EventArgs args) =>
            TryDispatchBatches();

        private void TaskStatisticsEventHandler(
            object? source, EventArgs args) =>
            TryDispatchBatches();

        private void OnCompleted(
            object? source,
            EventArgs args) =>
            _taskBacklog.Complete();
    }
}
