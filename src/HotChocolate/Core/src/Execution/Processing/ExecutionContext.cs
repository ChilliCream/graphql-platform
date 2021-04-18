using System;
using System.Threading.Tasks;
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

        public TaskScheduler TaskScheduler
        {
            get
            {
                AssertNotPooled();
                return _batchDispatcher.TaskScheduler;
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

        public IExecutionTaskContext TaskContext
        {
            get
            {
                AssertNotPooled();
                return _taskContext;
            }
        }

        private void OnCompleted(object? source, EventArgs args)
        {
            AssertNotPooled();
            _taskBacklog.Complete();
        }
    }
}
