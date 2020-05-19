using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.ObjectPool;
using HotChocolate.Execution.Utilities;
using HotChocolate.Fetching;

namespace HotChocolate.Execution
{
    internal partial class ExecutionContext 
        : IExecutionContext
    {
        public ITaskQueue Tasks => _taskQueue;

        public ITaskStatistics TaskStats => _taskStatistics;

        public bool IsCompleted => TaskStats.Enqueued == 0 && TaskStats.Running == 0;

        public ObjectPool<ResolverTask> TaskPool { get; }

        public IBatchDispatcher BatchDispatcher { get; private set; } = default!;

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
