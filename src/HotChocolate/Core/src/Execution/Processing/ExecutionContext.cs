using System;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Fetching;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Processing
{
    internal partial class ExecutionContext : IExecutionContext
    {
        public TaskScheduler TaskScheduler => _taskScheduler;

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

        private void BeginTryDispatchBatches() =>
            TryDispatchBatches();

        private async ValueTask TryDispatchBatches()
        {
            AssertNotPooled();

            if (TaskBacklog.IsEmpty && _taskScheduler.HasEmptyQueue && BatchDispatcher.HasTasks)
            {
                await Task.Yield();

                if (TaskBacklog.IsEmpty && _taskScheduler.HasEmptyQueue && BatchDispatcher.HasTasks)
                {
                    BatchDispatcher.Dispatch(Register);
                }
            }

            void Register(IExecutionTaskDefinition taskDefinition)
            {
                TaskBacklog.Register(taskDefinition.Create(_taskContext));
            }
        }

        private void BatchDispatcherEventHandler(
            object? source, EventArgs args) =>
            BeginTryDispatchBatches();

        private void TaskStatisticsEventHandler(
            object? source, EventArgs args) =>
            BeginTryDispatchBatches();

        private void OnCompleted(object? source, EventArgs args)
        {
            AssertNotPooled();
            // _taskBacklog.Complete();
        }
    }
}
