using System;
using HotChocolate.Fetching;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing
{
    internal partial class ExecutionContext : IExecutionContext
    {
        public ITaskBacklog TaskBacklog
        {
            get
            {
                AssertNotPooled();
                return _taskBacklog;
            }
        }

        public IDeferredTaskBacklog DeferredTaskBacklog
        {
            get
            {
                AssertNotPooled();
                return _deferredTaskBacklog;
            }
        }

        public ITaskStatistics TaskStats
        {
            get
            {
                AssertNotPooled();
                return _taskStatistics;
            }
        }

        public bool IsCompleted
        {
            get
            {
                AssertNotPooled();
                return TaskStats.IsCompleted;
            }
        }

        public ObjectPool<ResolverTask> TaskPool
        {
            get
            {
                AssertNotPooled();
                return _taskPool;
            }
        }

        public IBatchDispatcher BatchDispatcher
        {
            get
            {
                AssertNotPooled();
                return _batchDispatcher;
            }
        }

        private void TryDispatchBatches()
        {
            AssertNotPooled();

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

        private void OnCompleted(object? source, EventArgs args)
        {
            AssertNotPooled();
            _taskBacklog.Complete();
        }
    }
}
