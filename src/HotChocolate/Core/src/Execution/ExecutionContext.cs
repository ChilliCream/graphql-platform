using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Fetching;
using HotChocolate.Execution.Utilities;
using Microsoft.Extensions.ObjectPool;
using System;

namespace HotChocolate.Execution
{
    internal partial class ExecutionContext : IExecutionContext
    {
        private readonly TaskQueue _taskQueue;
        private readonly TaskStatistics _taskStatistics;
        private readonly object _engineLock = new object();
        private TaskCompletionSource<bool>? _waitForEngineTask;

        public ExecutionContext(
            ObjectPool<ResolverTask> taskPool,
            IBatchDispatcher batchDispatcher)
        {
            _taskStatistics = new TaskStatistics();
            _taskQueue = new TaskQueue(_taskStatistics, taskPool);
            TaskPool = taskPool;
            BatchDispatcher = batchDispatcher;
            BatchDispatcher.TaskEnqueued += BatchDispatcherEventHandler;
            TaskStats.StateChanged += TaskStatisticsEventHandler;
        }

        public ITaskQueue Tasks => _taskQueue;

        public ITaskStatistics TaskStats => _taskStatistics;

        public bool IsCompleted => TaskStats.Enqueued == 0 && TaskStats.Running == 0;

        public ObjectPool<ResolverTask> TaskPool { get; }

        public IBatchDispatcher BatchDispatcher { get; }

        public Task WaitForEngine(CancellationToken cancellationToken)
        {
            TaskCompletionSource<bool>? waitForEngineTask = _waitForEngineTask;
            if (waitForEngineTask == null)
            {
                return Task.CompletedTask;
            }

            cancellationToken.Register(() => waitForEngineTask.SetCanceled());
            return waitForEngineTask.Task;
        }

        private void SetEngineState()
        {
            lock (_engineLock)
            {
                if (TaskStats.Enqueued > 0 || BatchDispatcher.HasTasks || IsCompleted)
                {
                    // in case there is a task someone might be already waiting, 
                    // in this case we have to complete the task and clear it
                    if (_waitForEngineTask != null)
                    {
                        ResetTaskSource();
                    }
                }
                else
                {
                    // in case there is a task someone might be already waiting, 
                    // if there is no task we have to create one
                    if (_waitForEngineTask == null)
                    {
                        _waitForEngineTask = new TaskCompletionSource<bool>();
                    }
                }
            }
        }

        private void BatchDispatcherEventHandler(object? source, EventArgs args) =>
            SetEngineState();

        private void TaskStatisticsEventHandler(object? source, EventArgs args) =>
            SetEngineState();

        private void ResetTaskSource()
        {
            _waitForEngineTask?.SetResult(true);
            _waitForEngineTask = null;
        }
    }
}
